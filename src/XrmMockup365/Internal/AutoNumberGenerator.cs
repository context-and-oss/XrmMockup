using Microsoft.Xrm.Sdk.Metadata;
using DG.Tools.XrmMockup.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Security.Cryptography;
using System.Text;

namespace DG.Tools.XrmMockup.Internal
{
    /// <summary>Per-environment sequence state. Metadata and static caches never own counters.</summary>
    internal sealed class AutoNumberGenerator
    {
        internal const long DefaultSeed = 1000;
        // One lock protects allocation and every other access to this generator's state.
        private readonly object syncRoot = new object();
        private readonly Dictionary<Tuple<string, string>, AutoNumberStateDTO> states =
            new Dictionary<Tuple<string, string>, AutoNumberStateDTO>();

        internal void SetSeed(string entity, string attribute, long value)
        {
            lock (syncRoot)
            {
                if (value < 0) throw new FaultException("An auto-number seed cannot be negative.");
                states[Tuple.Create(entity, attribute)] = new AutoNumberStateDTO
                {
                    EntityName = entity, AttributeName = attribute, Seed = value, NextValue = value
                };
            }
        }

        internal long GetSeed(string entity, string attribute)
        {
            lock (syncRoot) return GetState(entity, attribute).Seed;
        }

        internal long GetNext(string entity, string attribute)
        {
            lock (syncRoot)
            {
                var state = GetState(entity, attribute);
                if (state.Exhausted) throw new FaultException("The auto-number sequence is exhausted.");
                return state.NextValue;
            }
        }

        // Caller must hold syncRoot; mutable state never escapes the generator.
        private AutoNumberStateDTO GetState(string entity, string attribute)
        {
            var key = Tuple.Create(entity, attribute);
            return states.TryGetValue(key, out var state) ? state : new AutoNumberStateDTO
            {
                EntityName = entity, AttributeName = attribute, Seed = DefaultSeed, NextValue = DefaultSeed
            };
        }

        internal string Generate(string entity, StringAttributeMetadata attribute, DateTime utcNow)
        {
            // Keep the read, full render, and advance atomic even across repeated placeholders.
            lock (syncRoot) return GenerateCore(entity, attribute, utcNow);
        }

        private string GenerateCore(string entity, StringAttributeMetadata attribute, DateTime utcNow)
        {
            // Parse and render the entire format before consuming a sequence. Invalid formats
            // must not partially advance it. Repeated SEQNUM placeholders share one reservation.
            var sequence = GetState(entity, attribute.LogicalName).NextValue;
            var usesSequence = false;
            var format = attribute.AutoNumberFormat;
            var result = new StringBuilder();
            for (var index = 0; index < format.Length;)
            {
                var character = format[index++];
                if (character == '}') throw InvalidFormat();
                if (character != '{') { result.Append(character); continue; }
                var end = format.IndexOf('}', index);
                if (end < 0) throw InvalidFormat();
                var token = format.Substring(index, end - index);
                if (token.Contains("{")) throw InvalidFormat();
                index = end + 1;
                var separator = token.IndexOf(':');
                if (separator <= 0 || separator == token.Length - 1) throw InvalidFormat();
                var kind = token.Substring(0, separator);
                var parameter = token.Substring(separator + 1);
                switch (kind)
                {
                    case "SEQNUM":
                        // A Dataverse string column cannot exceed 4000 characters. Bound padding
                        // as well as parsing the complete integer, including multi-digit widths.
                        var width = ParseLength(parameter, 4000);
                        sequence = GetNext(entity, attribute.LogicalName);
                        result.Append(sequence.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0'));
                        usesSequence = true;
                        break;
                    case "RANDSTRING":
                        var length = ParseLength(parameter, 6);
                        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                        using (var random = RandomNumberGenerator.Create())
                        {
                            var buffer = new byte[1];
                            for (var i = 0; i < length; i++)
                            {
                                // Reject the tail rather than introducing modulo bias.
                                do { random.GetBytes(buffer); } while (buffer[0] >= 252);
                                result.Append(alphabet[buffer[0] % alphabet.Length]);
                            }
                        }
                        break;
                    case "DATETIMEUTC":
                        try { result.Append(utcNow.ToString(parameter, CultureInfo.InvariantCulture)); }
                        catch (FormatException) { throw InvalidFormat(); }
                        break;
                    default:
                        throw InvalidFormat();
                }
            }

            if (usesSequence)
            {
                var state = GetState(entity, attribute.LogicalName);
                state.Exhausted = sequence == long.MaxValue;
                if (!state.Exhausted) state.NextValue = sequence + 1;
                states[Tuple.Create(entity, attribute.LogicalName)] = state;
            }
            return result.ToString();
        }

        private static int ParseLength(string parameter, int maximum)
        {
            if (!int.TryParse(parameter, NumberStyles.None, CultureInfo.InvariantCulture, out var length) ||
                length < 1 || length > maximum) throw InvalidFormat();
            return length;
        }

        private static FaultException InvalidFormat() => new FaultException("Invalid AutoNumberFormat.");

        internal List<AutoNumberStateDTO> Capture()
        {
            lock (syncRoot) return states.Values.Select(s => s.Clone()).ToList();
        }

        internal void Restore(IEnumerable<AutoNumberStateDTO> snapshot)
        {
            lock (syncRoot)
            {
                states.Clear();
                if (snapshot == null) return; // JSON snapshots produced before auto-number support.
                foreach (var state in snapshot)
                    states.Add(Tuple.Create(state.EntityName, state.AttributeName), state.Clone());
            }
        }
    }
}
