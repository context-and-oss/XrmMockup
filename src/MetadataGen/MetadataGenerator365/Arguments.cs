using DG.Tools;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Text.RegularExpressions;

namespace DG.Tools.XrmMockup.Metadata
{
    internal static class Arguments {

        public static ArgumentDescription Url = new ArgumentDescription() {
            Name = "url",
            Abbreviations = new string[] { }
        };

        public static ArgumentDescription Username = new ArgumentDescription() {
            Name = "username",
            Abbreviations = new string[] { "usr", "u" }
        };

        public static ArgumentDescription Password = new ArgumentDescription() {
            Name = "password",
            Abbreviations = new string[] { "pwd", "p" }
        };

        public static ArgumentDescription Domain = new ArgumentDescription() {
            Name = "domain",
            Abbreviations = new string[] { "dmn", "d" }
        };

        public static ArgumentDescription AuthProvider = new ArgumentDescription() {
            Name = "ap",
            Abbreviations = new string[] { "authenticationprovider", "provider" }
        };

        public static ArgumentDescription OutDir = new ArgumentDescription() {
            Name = "outDir",
            Abbreviations = new string[] { "out", "o" }
        };


        public static ArgumentDescription Entities = new ArgumentDescription() {
            Name = "entities",
            Abbreviations = new string[] { "es" }
        };

        public static ArgumentDescription Solutions = new ArgumentDescription() {
            Name = "solutions",
            Abbreviations = new string[] { "ss" }
        };

        public static ArgumentDescription UnitTestProjectPath = new ArgumentDescription() {
            Name = "projectPath",
            Abbreviations = new string[] { "pp" }
        };

        public static ArgumentDescription fetchFromAssemblies = new ArgumentDescription()
        {
            Name = "fetchFromAssemblies",
            Abbreviations = new string[] { "fa" }
        };

        public static ArgumentDescription Method = new ArgumentDescription()
        {
            Name = "method",
            Abbreviations = new string[] { }
        };

        public static ArgumentDescription ClientId = new ArgumentDescription()
        {
            Name = "mfaAppId",
            Abbreviations = new string[] { }
        };

        public static ArgumentDescription ReturnUrl = new ArgumentDescription()
        {
            Name = "mfaReturnUrl",
            Abbreviations = new string[] { }
        };

        public static ArgumentDescription ClientSecret = new ArgumentDescription()
        {
            Name = "mfaClientSecret",
            Abbreviations = new string[] { }
        };

        public static ArgumentDescription ConnectionString = new ArgumentDescription()
        {
            Name = "connectionString",
            Abbreviations = new string[] { }
        };

        public static ArgumentDescription MitigateDuplicateSecurityRoles = new ArgumentDescription()
        {
            Name = "mitigateDuplicateSecurityRoles",
            Abbreviations = new string[] { }
        };

        public static ArgumentDescription[] ArgList = new ArgumentDescription[] {
            Url,
            Username,
            Password,
            Domain,
            AuthProvider,
            Entities,
            Solutions,
            OutDir,
            UnitTestProjectPath,
            fetchFromAssemblies,
            Method,
            ClientId,
            ReturnUrl,
            ClientSecret,
            ConnectionString,
            MitigateDuplicateSecurityRoles
        };
    }
}
