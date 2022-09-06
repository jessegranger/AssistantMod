namespace Assistant {
	partial class Assistant {
		public class MovingAverage {
			public double Value = 0f;
			public int Period;
			public MovingAverage(int period) => Period = period;
			public void Add(double sample) => Value = ((Value * (Period - 1)) + sample) / Period;
		}
	}
}
