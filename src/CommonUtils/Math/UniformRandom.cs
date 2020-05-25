using System;

namespace CommonUtils.MathLib
{
    /// <summary>
    /// An uniform random number generator 
    /// Generated random values will be in the [lower, upper) interval
    /// </summary>
    public class UniformRandom
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        /// <param name="rand"></param>
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
        /// Returns an uniform random double number
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
