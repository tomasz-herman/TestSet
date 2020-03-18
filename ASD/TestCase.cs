using System;

namespace ASD
{
	[Serializable]
	public abstract class TestCase
	{
		public double TimeLimit { get; }
		public Exception ExpectedException { get; }
		public string Description { get; }
		public bool Timeout { get; internal set; }
		public Exception ThrownException { get; internal set; }
		public double PerformanceTime { get; internal set; }
		public Result ResultCode { get; internal set; }
		public string Message { get; internal set; }

		protected TestCase(double timeLimit, Exception expectedException, string description)
		{
			TimeLimit = timeLimit;
			ExpectedException = expectedException;
			Description = description;
		}

		protected internal abstract void PerformTestCase(object prototypeObject);

		protected internal abstract (Result resultCode, string message) VerifyTestCase(object settings);

		public enum Result
		{
			NotPerformed,
			Success,
			ExpectedExceptionThrown,
			LowEfficiency,
			Timeout,
			ExceptionNotThrown,
			IncorrectExceptionThrown,
			UnexpectedExceptionThrown,
			UnexpectedProgramTermination,
			WrongResult
		}
	}
}
