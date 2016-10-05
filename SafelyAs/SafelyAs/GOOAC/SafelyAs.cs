namespace GOOAC.Lib
{
    public static class To<I2> where I2 : class
    {
        public static I2 Safely<I1>(I1 from)
            where I1 : class
        {
            return from as I2;
        }
    }
}
