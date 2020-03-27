﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Oryx.BuildScriptGenerator.Tests
{
    public class TestConfiguration : IConfiguration
    {
        private readonly Dictionary<string, string> _values;

        public TestConfiguration()
        {
            _values = new Dictionary<string, string>();
        }

        public string this[string key]
        {
            get
            {
                if (_values.ContainsKey(key))
                {
                    return _values[key];
                }
                return string.Empty;
            }
            set
            {
                _values[key] = value;
            }
        }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            throw new NotImplementedException();
        }

        public IChangeToken GetReloadToken()
        {
            throw new NotImplementedException();
        }

        public IConfigurationSection GetSection(string key)
        {
            throw new NotImplementedException();
        }
    }
}
