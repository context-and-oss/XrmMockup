
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Text.RegularExpressions;

namespace DG.Tools.XrmMockup.Metadata
{
    internal class ArgumentParser {

        private Regex argRegex = new Regex("^[/\\-]?([^:=]+)((:|=)\"?(.*?)\"?)?$");
        private Dictionary<string, ArgumentDescription> ArgToArgDesc = new Dictionary<string, ArgumentDescription>();

        private Dictionary<string, string> ArgMap = new Dictionary<string, string>();
        private Dictionary<ArgumentDescription, string> ArgDescMap = new Dictionary<ArgumentDescription, string>();


        public ArgumentParser(IEnumerable<ArgumentDescription> possibleArgs, IEnumerable<string> args) {
            // Build up abbreviation map to easily look up the ArgumentDescription of given arguments
            foreach (var argDesc in possibleArgs) {
                ArgToArgDesc[argDesc.Name.ToLower()] = argDesc;
                foreach (var abbrv in argDesc.Abbreviations) {
                    ArgToArgDesc[abbrv.ToLower()] = argDesc;
                }
            }

            // Add arguments from config file
            foreach (var key in ConfigurationManager.AppSettings.AllKeys) {
                AddArg(key, ConfigurationManager.AppSettings[key]);
            }

            // Parse command-line args
            foreach (var arg in args) {
                ParseArg(arg);
            }
        }

        private void ParseArg(string arg) {
            var match = argRegex.Match(arg.Trim());
            if (match.Success) {
                var argName = match.Groups[1].Value;
                var argVal = match.Groups[4].Value;
                AddArg(argName, argVal);
            }
        }

        private void AddArg(string argName, string argVal) {
            var lowerArgName = argName.ToLower();
            ArgMap[lowerArgName] = argVal;

            if (ArgToArgDesc.TryGetValue(lowerArgName, out ArgumentDescription argDesc)) {
                ArgDescMap[argDesc] = argVal;
            }
        }

        public string this[string argName] {
            get {
                if (!ArgMap.TryGetValue(argName.ToLower(), out string val)) {
                    return ConfigurationManager.AppSettings[argName];
                }
                if (val is string s) return s;

                return null;
            }
        }

        public string this[ArgumentDescription argDesc] {
            get {
                if (!ArgDescMap.TryGetValue(argDesc, out string val)) {
                    return ConfigurationManager.AppSettings[argDesc.Name];
                }
                return val;
            }
        }

        public T GetAsType<T>(ArgumentDescription argDesc) {
            string value = this[argDesc];
            try {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                return (T)(converter.ConvertFromInvariantString(value));
            }
            catch (NotSupportedException) {
                return default;
            }
        }
    }

    class ArgumentDescription {
        public string Name;
        public string[] Abbreviations;

        public override int GetHashCode() {
            return Name.GetHashCode();
        }

        public override bool Equals(Object obj) {
            if (obj is ArgumentDescription description) return Name == description.Name;

            return false;
        }

        public override string ToString() {
            return Name;
        }
    }
}
