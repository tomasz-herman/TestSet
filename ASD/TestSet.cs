using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ASD
{
    [Serializable]
    public class TestSet
    {
        public string Description { get; private set; }
        public double SpeedFactor { get; private set; }
        public int PassedCount { get; private set; }
        public int LowEfficiencyCount { get; private set; }
        public int FailedCount { get; private set; }
        public int TimeoutsCount { get; private set; }
        public Type PrototypeType => _prototypeObject.GetType();

        public TestSet(object prototypeObject, string description, Action speedFactorMeasureFunction = null, object settings = null, int stackSize = 1)
        {
            _prototypeObject = prototypeObject;
            Description = description;
            SpeedFactor = TimeLimitRunner.CalculateSpeedFactor(speedFactorMeasureFunction);
            _speedFactorMeasureFunction = speedFactorMeasureFunction;
            _settings = settings;
            _stackSize = stackSize;
        }

        public void PerformTests(bool checkTimeLimit, bool verbose = true)
        {
            TimeoutsCount = 0;
            FailedCount = 0;
            LowEfficiencyCount = 0;
            PassedCount = 0;
            Console.WriteLine("\n" + Description);
            for (var i = 0; i < TestCases.Count; i++)
            {
                Console.Write("Test {0,2}:  ", i + 1);
                var testResult = PerformSingleTest(TestCases[i], _prototypeObject, checkTimeLimit, verbose);
                Console.WriteLine(verbose ? TestCases[i].Message : testResult);
            }
            Console.WriteLine("\nTests completed");
            Console.WriteLine("  {0,2}/{1,2} passed ({2} low efficiency)", PassedCount, TestCases.Count, LowEfficiencyCount);
            Console.WriteLine("  {0,2}/{1,2} failed ({2} timeout)", FailedCount, TestCases.Count, TimeoutsCount);
            Console.WriteLine();
        }

        private string PerformSingleTest(TestCase testCase, object proxyPrototypeObject, bool checkTimeLimit, bool verbose)
        {
            testCase.PerformanceTime = TimeLimitRunner.Run(SpeedFactor * testCase.TimeLimit, checkTimeLimit, testCase.ExpectedException, out var timeout, out var thrownException,
                () => testCase.PerformTestCase(proxyPrototypeObject), _stackSize) / SpeedFactor;
            testCase.Timeout = timeout;
            testCase.ThrownException = thrownException;
            string result;
            if (testCase.ThrownException != null)
            {
                if (testCase.ExpectedException == null)
                {
                    testCase.ResultCode = TestCase.Result.UnexpectedExceptionThrown;
                    testCase.Message = testCase.ThrownException.Message;
                    FailedCount++;
                    result = "unexpected exception";
                }
                else if (testCase.ThrownException.GetType() != testCase.ExpectedException.GetType())
                {
                    testCase.ResultCode = TestCase.Result.IncorrectExceptionThrown;
                    testCase.Message = testCase.ThrownException.Message;
                    FailedCount++;
                    result = "incorrect exception";
                }
                else
                {
                    testCase.ResultCode = TestCase.Result.ExpectedExceptionThrown;
                    testCase.Message = $"OK, expected exception of type {testCase.ThrownException.GetType()} thrown";
                    PassedCount++;
                    result = "OK, expected exception thrown";
                }
            }
            else if (testCase.ExpectedException != null)
            {
                testCase.ResultCode = TestCase.Result.ExceptionNotThrown;
                testCase.Message = $"error, expected exception of type {testCase.ExpectedException.GetType()} not thrown";
                FailedCount++;
                result = "error, expected exception not thrown";
            }
            else if (testCase.Timeout)
            {
                testCase.ResultCode = TestCase.Result.Timeout;
                testCase.Message = $"computation interrupted (time limit {testCase.TimeLimit} time units exceeded)";
                TimeoutsCount++;
                FailedCount++;
                result = "timeout";
            }
            else
            {
                (testCase.ResultCode, testCase.Message) = testCase.VerifyTestCase(_settings);
                if (testCase.ResultCode < TestCase.Result.WrongResult && testCase.ResultCode != TestCase.Result.Success && testCase.ResultCode != TestCase.Result.LowEfficiency)
                {
                    throw new Exception("Test engine error: invalid result code");
                }
                if (testCase.Message == null || testCase.Message.Trim() == string.Empty)
                {
                    throw new Exception("Test engine error: invalid result message");
                }
                if (testCase.ResultCode != TestCase.Result.Success)
                {
                    if (testCase.ResultCode != TestCase.Result.LowEfficiency)
                    {
                        if (testCase.ResultCode != TestCase.Result.WrongResult)
                        {
                            FailedCount++;
                            result = "undescribed error";
                        }
                        else
                        {
                            FailedCount++;
                            result = "wrong result";
                        }
                    }
                    else
                    {
                        LowEfficiencyCount++;
                        PassedCount++;
                        result = "low efficiency";
                    }
                }
                else
                {
                    PassedCount++;
                    result = "OK";
                }
            }
            return !verbose ? result : testCase.Message;
        }

        [OnDeserialized]
        private void OnDeserializedCalculateSpeedFactor(StreamingContext context)
        {
            SpeedFactor = TimeLimitRunner.CalculateSpeedFactor(_speedFactorMeasureFunction);
        }

        public List<TestCase> TestCases = new List<TestCase>();

        private object _prototypeObject;

        private Action _speedFactorMeasureFunction;

        private object _settings;

        private int _stackSize;
    }
}
