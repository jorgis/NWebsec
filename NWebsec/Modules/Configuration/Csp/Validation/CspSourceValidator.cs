﻿// Copyright (c) André N. Klingsheim. See License.txt in the project root for license information.

using System;
using System.Configuration;
using System.Text.RegularExpressions;

namespace NWebsec.Modules.Configuration.Csp.Validation
{
    class CspSourceValidator : ConfigurationValidatorBase
    {
        private static readonly string SchemeRegex = "^[a-zA-Z]*[a-zA-Z0-9" + Regex.Escape("+.-") + "]:$";
        private const string HostRegex = @"^(\*\.)?([a-zA-Z0-9\-]+)(\.[a-zA-Z0-9\-]+)*$";

        public override bool CanValidate(Type type)
        {
            return type == typeof(String);
        }

        public override void Validate(object value)
        {
            var source = (string)value;
            
            if (String.IsNullOrEmpty(source)) return;

            if (source.Equals("*")) return;

            if (Uri.IsWellFormedUriString(source, UriKind.Absolute)) return;

            if (Regex.IsMatch(source, SchemeRegex)) return;

            var index = source.IndexOf("//", StringComparison.Ordinal);

            string hostString;
            if (index > 0)
            {
                var scheme = source.Substring(0, index);
                if (!Regex.IsMatch(scheme, SchemeRegex))
                    throw new InvalidCspSourceException("Invalid scheme in source: " + source);
                hostString = source.Substring(index + 2, source.Length - (index + 2));
            }
            else
            {
                hostString = source;
            }

            if (!ValidateHostString(hostString))
                throw new InvalidCspSourceException("Invalid host in source: " + source);
        }

        private bool ValidateHostString(string host)
        {
            char[] pathSplit = { '/' };
            var hostParts = host.Split(pathSplit, 2);
            var actualHost = hostParts[0];
            
            char[] portSplit = { ':' };
            var actualHostParts = actualHost.Split(portSplit, 2);

            if (actualHostParts.Length == 2 && !ValidatePort(actualHostParts[1]))
                return false;

            return Regex.IsMatch(actualHostParts[0], HostRegex);
        }

        private bool ValidatePort(string port)
        {
            if (port.Equals("*")) return true;

            int portNumber;
            return Int32.TryParse(port, out portNumber);
        }
    }

    [Serializable]
    public class InvalidCspSourceException : ConfigurationErrorsException
    {
        public InvalidCspSourceException(string s) : base(s)
        {
        }
    }
}