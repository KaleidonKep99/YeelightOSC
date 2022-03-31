namespace YeelightOSC
{
    public class MathFuncs
    {
        // Console.Write(String.Format("{0:X6}", FtoI(r) << 16 | FtoI(g) << 8 | FtoI(b)));
        public int FtoI(float Value, int MaxI, float? MaxF = 1.0f)
        {
            return (int)(Value >= MaxF ? MaxI : Value * (MaxI + 1));
        }

        public int ItoRi(int Value, int Min, int Max)
        {
            return (Value - Min) * 100 / (Max - Min);
        }

        public float ItoRf(int Value, float Min, float Max)
        {
            return (Value - Min) * 1.0f / (Max - Min);
        }

        public float Lerp(float Value, float Min, float Max)
        {
            return Min * (1 - Value) + Max * Value;
        }
    }
}