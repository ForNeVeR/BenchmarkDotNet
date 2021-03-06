﻿using System.Collections.Generic;

namespace BenchmarkDotNet.Samples.Framework
{
    // From https://github.com/dotnet/coreclr/issues/1579
    public class Framework_DictionaryVsIDictionary
    {
        Dictionary<string, string> dict;
        IDictionary<string, string> idict;

        [Setup]
        public void Setup()
        {
            dict = new Dictionary<string, string>();
            idict = (IDictionary<string, string>)dict;
        }

        [Benchmark]
        public Dictionary<string, string> DictionaryEnumeration()
        {
            // Doesn't allocate
            foreach (var item in dict)
            {
                ;
            }
            return dict;
        }

        [Benchmark]
        public IDictionary<string, string> IDictionaryEnumeration()
        {
            // Allocates 998k
            foreach (var item in idict)
            {
                ;
            }
            return idict;
        }
    }
}
