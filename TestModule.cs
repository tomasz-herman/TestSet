using System;
using System.Collections.Generic;

namespace ASD
{
    [Serializable]
    public abstract class TestModule
    {
        public abstract void PrepareTestSets();

        public virtual double ScoreResult() { return 1.0; }

        public Dictionary<string, TestSet> TestSets = new Dictionary<string, TestSet>();
    }
}
