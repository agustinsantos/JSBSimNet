using System;

namespace CommonUtils.MathLib
{
    public class UniformRandom
    {
        public UniformRandom(double lower, double upper, Random rand)
        {
            this.lower = lower;
            this.upper = upper;

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
            double randomVal = rand.NextDouble(); //uniform[0,1) random doubles
            return randomVal * (upper - lower) + lower; ;
        }
        protected readonly double lower;
        protected readonly double upper;
        protected readonly Random rand;
    }

}
