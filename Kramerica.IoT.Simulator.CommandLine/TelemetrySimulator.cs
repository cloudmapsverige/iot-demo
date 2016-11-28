using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kramerica.IoT.Simulator.CommandLine
{
    public class TelemetrySimulator
    {
        private Random _random;
        private double current;
        private double volatility;

        public TelemetrySimulator(int startMin, int startMax, double volatility)
        {
            _random = new Random(Guid.NewGuid().GetHashCode());
            current = _random.Next(startMin, startMax);
            this.volatility = volatility;
        }

        public double GetNextSimulatedValue()
        {
            var rnd = _random.NextDouble();
            var changePercent = 2 * volatility * rnd;
            if (changePercent > volatility)
                changePercent -= (2 * volatility);
            current = (current * changePercent) + current;
            return Math.Round(current, 2);
        }

    }
}
