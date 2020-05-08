using System;

namespace CommonUtils.MathLib
{
    public class NormalRandom
    {
        public NormalRandom(double mean, double stdDev, Random rand)
        {
            this.mean = mean;
            this.stdDev = stdDev;

            if (rand != null)
                this.rand = rand;
            else
                this.rand = new Random();
        }
        /// <summary>
        /// Returns a random double number
        /// It has an implementation of the Box-Muller transform.
        /// </summary>
        /// <returns></returns>
        public double Next()
        {
            double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
            double u2 = 1.0 - rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            double randNormal =
                         mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)
            return randNormal;
        }
        protected readonly double mean;
        protected readonly double stdDev;
        protected readonly Random rand;
    }

}
