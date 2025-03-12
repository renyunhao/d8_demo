// This is collection of useful static math functions for packing/unpacking and scaling.
// Note that "new" with constructors is avoided here in many functions because it's been shown to be slower.
//--------------------------------------------------------------------------------------------------//


using Unity.Mathematics;

namespace AnimCooker
{
    public static class PackingUtils
    {
        public const float INV_BYTE = 1f / 255f; // inverse byte
        static float4 bitEnc = new float4(1f, 255f, 65025f, 16581375f);
        static float4 bitMsk = new float4(1f / 255f, 1f / 255f, 1f / 255f, 0f);
        static float4 bitDec = 1.0f / bitEnc;

        // given val, min, and max, produces a packed value stuffed into a Vector4, where x is r, y is g, z is b, and w is a.
        // note that z and x will be stored with 11 bits, and y will be stored with 10 bits.
        // each of the resulting vector's components will be between 0 and 1
        // 00 01 02 03 04 05 06 07 08 09 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31
        // |--------------11 bits (x)-----| |-----------10 bits (y)-----| |----------11 bits (z) --------|
        public static float4 PackToR11G10B11(float3 val, float minVal, float maxVal)
        {
            uint scaledX = (uint)Scale(val.x, minVal, maxVal, 0f, 2047.999f); // x is 11 bits
            uint scaledY = (uint)Scale(val.y, minVal, maxVal, 0f, 1023.999f); // y is only 10 bits
            uint scaledZ = (uint)Scale(val.z, minVal, maxVal, 0f, 2047.999f); // z is 11 bits

            // X11Y10Z11
            uint x = scaledX << 21; // 32 - 11 --> 21, moves it to first 10 bits
            uint y = scaledY << 11; // 21 - 10 --> 11, moves it to middle 10 bits
            uint z = scaledZ; // last 11 bits
            uint packed = (x | y | z);
            return PackUintToRgba(packed);
        }

        // val --> 3 float values that will be scaled where x=r, y=g, z=b
        // minVal --> the smallest value in val
        // maxVal --> the largest value in val
        // return --> a float4 containing values between 0 and 1
        public static float4 ScaleThreeFloatsToFloat4(float3 val, float minVal, float maxVal)
        {
            float3 scaled = Scale(val, minVal, maxVal, 0, 1);
            float4 ret;
            ret.x = scaled.x;
            ret.y = scaled.y;
            ret.z = scaled.z;
            ret.w = 0.0f;
            return ret;
        }

        public static float4 ScaleFourFloatsToFloat4(float4 val, float minVal, float maxVal)
        {
            return Scale(val, minVal, maxVal, 0, 1);
        }

        public static float4 PackThreeFloatsToRgb24AsFloat4(float3 val, float minVal, float maxVal)
        {
            float3 ret = Scale(val, minVal, maxVal, 0, 255);
            return new float4(ret.x, ret.y, ret.z, 1.0f);
        }

        public static half3 PackThree5BitFloatsToRgbHalf(float3 val, float minVal, float maxVal)
        {
            ushort scaledX = (ushort)Scale(val.x, minVal, maxVal, 0f, 31.999f); // x is 5 bits
            ushort scaledY = (ushort)Scale(val.y, minVal, maxVal, 0f, 31.999f); // y is 5 bits
            ushort scaledZ = (ushort)Scale(val.z, minVal, maxVal, 0f, 31.999f); // z is 5 bits

            ushort x = (ushort)(scaledX << 5);
            ushort y = (ushort)(scaledY << 5);
            ushort z = scaledZ;
            ushort packed = (ushort)(x | y | z);
            return PackUshortToRgbHalf(packed);
        }

        public static float4 PackSix5BitFloatsToArgb(float3 val1, float3 val2, float minVal, float maxVal)
        {
            uint scaledX1 = (uint)Scale(val1.x, minVal, maxVal, 0f, 31.999f); // x is 5 bits
            uint scaledY1 = (uint)Scale(val1.y, minVal, maxVal, 0f, 31.999f); // y is 5 bits
            uint scaledZ1 = (uint)Scale(val1.z, minVal, maxVal, 0f, 31.999f); // z is 5 bits

            uint scaledX2 = (uint)Scale(val2.x, minVal, maxVal, 0f, 31.999f); // x is 5 bits
            uint scaledY2 = (uint)Scale(val2.y, minVal, maxVal, 0f, 31.999f); // y is 5 bits
            uint scaledZ2 = (uint)Scale(val2.z, minVal, maxVal, 0f, 31.999f); // z is 5 bits

            // X5Y5Z5X5Y5Z5
            uint x1 = scaledX1 << 27; // 32 - 5 --> 27
            uint y1 = scaledY1 << 22; // 27 - 5 --> 22
            uint z1 = scaledZ1 << 17; // 22 - 5 --> 17
            uint x2 = scaledX2 << 12; // 17 - 5 --> 12
            uint y2 = scaledY2 << 7; // 12 - 5 --> 7
            uint z2 = scaledZ2;
            uint packed = (x1 | y1 | z1 | x2 | y2 | z2);
            return PackUintToRgba(packed);
        }

        // tangent is a value where x, y, and z are part of a vector, and the sign of w indicates the direction.
        // i am pretty sure that tangent is normalized to be between -1 and 1
        // x, y, and z will have 10 bits. w will use two bits.
        public static float4 PackTangentToArgb(float4 val, float minVal, float maxVal)
        {
            sbyte sign = (sbyte)val.z;
            uint scaledX = (uint)Scale(val.x, minVal, maxVal, 0, 1023.999f);
            uint scaledY = (uint)Scale(val.y, minVal, maxVal, 0, 1023.999f);
            uint scaledZ = (uint)Scale(val.z, minVal, maxVal, 0, 1023.999f);

            // X10Y10Z10W2 (W is sign)
            uint x = scaledX << 22; // 32 - 10 --> 22, moves it to first 10 bits
            uint y = scaledY << 12; // 22 - 10 --> 12, moves it to the next 10 bits
            uint z = scaledZ << 2; // 12 - 10 --> 2, moves it to the next 10 bits

            // will be either -1 or 1.
            // to avoid a branch, we scale w from unit range (-1..1) to unit interval (0..1).
            // (-1 + 1) * 0.5 --> 0
            // (1 + 1) * 0.5 --> 1
            uint w = (uint)ScaleUnitRangeToUnitInterval(val.z); // last two bytes

            // combine the values and return them as a packed RGBA
            uint packed = (x | y | z | w);
            return PackUintToRgba(packed);
        }

        // same as above, but with individual parameters for values
        public static float4 PackThree10BitFloatsToArgb(float val1, float val2, float val3, float minVal, float maxVal)
        {
            float3 val;
            val.x = val1;
            val.y = val2;
            val.z = val3;
            return PackToR11G10B11(val, minVal, maxVal);
            //return PackThree10BitFloatsToArgb(new float3(val1, val2, val3), minVal, maxVal);
        }

        // pack one 32 bit float into an RGBA vector.
        // the RGBA values will be between 0 and 1.
        public static float4 PackOne32bitFloatToRgba(float val, float min, float max)
        {
            float scaled = ScaleToUnitInterval(val, min, max);
            float4 enc = bitEnc * scaled;
            enc = math.frac(enc);
            enc -= enc.yzww * bitMsk;
            return enc;
        }

        // unpacks a single RGB value to a float, given a range
        public static float UnpackrgbaToOne32bitFloat(float4 val, float min, float max)
        {
            float unscaled = math.dot(val, bitDec);
            return ScaleFromUnitInterval(unscaled, min, max);
        }

        // pack two 16 bit floats into an RGBA vector
        // the RGBA values will be between 0 and 1
        public static float4 PackTwo16bitFloatsToRgba(float val1, float val2)
        {
            return PackUintToRgba((math.f32tof16(val1) << 16) | math.f32tof16(val2));
        }

        // same as the over version except that the value is a float2
        public static float4 PackTwo16bitFloatsToRgba(float2 val)
        {
            return PackTwo16bitFloatsToRgba(val.x, val.y);
        }

        // unpack an rgba vector into two 16 bit floats
        // the RGBA vector must have component values that are between 0 and 1
        public static float2 UnpackRgbaToTwo16bitFloats(float4 val)
        {
            uint input = UnpackRgbaToUint(val);
            float2 ret;
            ret.x = math.f16tof32(input >> 16);
            ret.y = math.f16tof32(input & 0xFFFF);
            return ret;
        }

        // pack four 8 bit bytes into an RGBA vector.
        // basically, it just multiplies each value by 1/255
        public static float4 PackFourBytesToRgba(byte val1, byte val2, byte val3, byte val4)
        {
            float4 ret;
            ret.x = val1;
            ret.y = val2;
            ret.z = val3;
            ret.w = val4;
            return ret * INV_BYTE;
        }

        // scales val, which is between oldMin and oldMax, to a number between newMin and newMax
        public static float Scale(float val, float oldMin, float oldMax, float newMin, float newMax)
        {
            return (((val - oldMin) / (oldMax - oldMin)) * (newMax - newMin)) + newMin;
        }

        // scales val, which is between oldMin and oldMax, to a vector between newMin and newMax
        public static float3 Scale(float3 val, float3 oldMin, float3 oldMax, float3 newMin, float3 newMax)
        {
            float3 ret;
            ret.x = Scale(val.x, oldMin.x, oldMax.x, newMin.x, newMax.x);
            ret.y = Scale(val.y, oldMin.y, oldMax.y, newMin.y, newMax.y);
            ret.z = Scale(val.z, oldMin.z, oldMax.z, newMin.z, newMax.z);
            return ret;
        }

        // scales val, which is between oldMin and oldMax, to a vector between newMin and newMax
        public static float4 Scale(float4 val, float4 oldMin, float4 oldMax, float4 newMin, float4 newMax)
        {
            float4 ret;
            ret.x = Scale(val.x, oldMin.x, oldMax.x, newMin.x, newMax.x);
            ret.y = Scale(val.y, oldMin.y, oldMax.y, newMin.y, newMax.y);
            ret.z = Scale(val.z, oldMin.z, oldMax.z, newMin.z, newMax.z);
            ret.w = Scale(val.w, oldMin.w, oldMax.w, newMin.w, newMax.w);
            return ret;
        }

        // returns val scaled between 0 and 1
        // val must be between oldMin and oldMax
        public static float ScaleToUnitInterval(float val, float oldMin, float oldMax)
        {
            return (val - oldMin) / (oldMax - oldMin);
        }
        // returns val scaled between 0 and 1
        // val must be between oldMin and oldMax
        public static float ScaleToUnitInterval(float val, float oldMax)
        {
            return val / oldMax;
        }
        // returns val scaled between 0 and 1
        // val must be between oldMin and oldMax
        public static float3 ScaleToUnitInterval(float3 val, float3 oldMin, float3 oldMax)
        {
            float3 ret;
            ret.x = ScaleToUnitInterval(val.x, oldMin.x, oldMax.x);
            ret.y = ScaleToUnitInterval(val.y, oldMin.y, oldMax.y);
            ret.z = ScaleToUnitInterval(val.z, oldMin.z, oldMax.z);
            return ret;
        }
        // returns val scaled between 0 and 1
        // val must be between oldMin and oldMax
        public static float3 ScaleToUnitInterval(float3 val, float3 oldMax)
        {
            float3 ret;
            ret.x = ScaleToUnitInterval(val.x, oldMax.x);
            ret.y = ScaleToUnitInterval(val.y, oldMax.y);
            ret.z = ScaleToUnitInterval(val.z, oldMax.z);
            return ret;
        }

        // returns val scaled between -1 and 1
        // val must be between oldMin and oldMax
        public static float ScaleToUnitRange(float val, float oldMin, float oldMax)
        {
            return (((val - oldMin) / (oldMax - oldMin)) * 2) - 1;
        }
        // returns val scaled between -1 and 1
        // val must be between 0 and oldMax
        public static float ScaleToUnitRange(float val, float oldMax)
        {
            return ((val / oldMax) * 2) - 1;
        }

        // returns val scaled between -1 and 1
        // val must be between 0 and 1
        public static float3 ScaleUnitIntervalToUnitRange(float3 val)
        {
            float3 ret;
            ret.x = ScaleUnitIntervalToUnitRange(val.x);
            ret.y = ScaleUnitIntervalToUnitRange(val.y);
            ret.z = ScaleUnitIntervalToUnitRange(val.z);
            return ret;
        }

        // returns val scaled betweeen -1 and 1
        // val must be between 0 and 1
        public static float ScaleUnitIntervalToUnitRange(float val)
        {
            //return ((val / 1) * 2) - 1;
            return (val * 2) - 1;
        }

        // returns val scaled between -1 and 1
        // val must be between 0 and oldMax
        public static float3 ScaleToUnitRange(float3 val, float3 oldMax)
        {
            float3 ret;
            ret.x = ScaleToUnitRange(val.x, oldMax.x);
            ret.y = ScaleToUnitRange(val.y, oldMax.y);
            ret.z = ScaleToUnitRange(val.z, oldMax.z);
            return ret;
        }
        // returns val scaled between -1 and 1
        // val must be between oldMin and oldMax
        public static float3 ScaleToUnitRange(float3 val, float3 oldMin, float3 oldMax)
        {
            float3 ret;
            ret.x = ScaleToUnitRange(val.x, oldMin.x, oldMax.x);
            ret.y = ScaleToUnitRange(val.y, oldMin.y, oldMax.y);
            ret.z = ScaleToUnitRange(val.z, oldMin.z, oldMax.z);
            return ret;
        }


        // returns val scaled between newMin and newMax
        // val MUST be between -1 and 1
        public static float ScaleFromUnitRange(float val, float newMin, float newMax)
        {
            return (((val + 1) * 0.5f) * (newMax - newMin)) + newMin;
        }
        // returns val scaled between 0 and newMax
        // val MUST be between -1 and 1
        public static float ScaleFromUnitRange(float val, float newMax)
        {
            return ((val + 1) * 0.5f) * newMax;
        }
        // returns val scaled between newMin and newMax
        // val MUST be between -1 and 1
        public static float3 ScaleFromUnitRange(float3 val, float3 newMin, float3 newMax)
        {
            float3 ret;
            ret.x = ScaleFromUnitRange(val.x, newMin.x, newMax.x);
            ret.y = ScaleFromUnitRange(val.y, newMin.y, newMax.y);
            ret.z = ScaleFromUnitRange(val.z, newMin.z, newMax.z);
            return ret;
        }
        // returns val scaled between newMin and newMax
        // val MUST be between -1 and 1
        public static float3 ScaleFromUnitRange(float3 val, float3 newMax)
        {
            float3 ret;
            ret.x = ScaleFromUnitRange(val.x, newMax.x);
            ret.y = ScaleFromUnitRange(val.y, newMax.y);
            ret.z = ScaleFromUnitRange(val.z, newMax.z);
            return ret;
        }

        // returns val scaled between newMin and newMax
        // val MUST be between 0 and 1
        public static float ScaleFromUnitInterval(float val, float newMin, float newMax)
        {
            return (val * (newMax - newMin)) + newMin;
        }
        // returns val scaled between newMin and newMax
        // val MUST be between 0 and 1
        public static float ScaleFromUnitInterval(float val, float newMax)
        {
            return val * newMax;
        }
        // returns val scaled between newMin and newMax
        // val MUST be between 0 and 1
        public static float3 ScaleFromUnitInterval(float3 val, float3 newMin, float3 newMax)
        {
            float3 ret;
            ret.x = ScaleFromUnitInterval(val.x, newMin.x, newMax.x);
            ret.y = ScaleFromUnitInterval(val.y, newMin.y, newMax.y);
            ret.z = ScaleFromUnitInterval(val.z, newMin.z, newMax.z);
            return ret;
        }
        // returns val scaled between newMin and newMax
        // val MUST be between 0 and 1
        public static float3 ScaleFromUnitInterval(float3 val, float3 newMax)
        {
            float3 ret;
            ret.x = ScaleFromUnitInterval(val.x, newMax.x);
            ret.y = ScaleFromUnitInterval(val.y, newMax.y);
            ret.z = ScaleFromUnitInterval(val.z, newMax.z);
            return ret;
        }

        // returns val scaled between 0 and 1
        // val must be between -1 and 1
        public static float ScaleUnitRangeToUnitInterval(float val)
        {
            return (val + 1) * 0.5f;
        }

        public static float3 ScaleUnitRangeToUnitInterval(float3 val)
        {
            float3 ret;
            ret.x = ScaleUnitRangeToUnitInterval(val.x);
            ret.y = ScaleUnitRangeToUnitInterval(val.y);
            ret.z = ScaleUnitRangeToUnitInterval(val.z);
            return ret;
        }

        // RRGGBBAA
        // multiplying by 1/255 converts the return to numbers between 0 and 1
        public static float4 PackUintToRgba(uint val)
        {
            float4 ret;
            ret.x = (float)(val >> 24) * INV_BYTE; // 000000RR
            ret.y = (float)((val >> 16) & 0xFF) * INV_BYTE; // 000000GG
            ret.z = (float)((val >> 8) & 0xFF) * INV_BYTE; // 000000BB
            ret.w = (float)(val & 0xFF) * INV_BYTE; // 000000AA
            return ret;
        }

        // RRGGBB
        public static half3 PackUshortToRgbHalf(ushort val)
        {
            half3 ret;
            ret.x = (half)(val >> 11); // 0000RR
            ret.y = (half)(val >> 6); // 0000GG
            ret.z = (half)((byte)val);
            return ret;
        }

        // unpacks an rgba vector to a uint
        public static uint UnpackRgbaToUint(float4 val)
        {
            return (((uint)math.round(val.x * 255)) << 24) | (((uint)math.round(val.y * 255)) << 16) | (((uint)math.round(val.z * 255)) << 8) | ((uint)math.round(val.w * 255));
        }

        // given a packed value and the min and max values of val (before they were scaled)
        // returns a de-scaled float3 with precision such that z and x are 11 bits, and y is 10 bits.
        // 00 01 02 03 04 05 06 07 08 09 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31
        // |--------------11 bits (x)-----| |-----------10 bits (y)-----| |----------11 bits (z) --------|
        public static float3 UnpackRgbaToThree10BitFloats(float4 val, float min, float max)
        {
            uint packed = UnpackRgbaToUint(val);
            uint x = (packed >> 21); // first 11 bits (0xFFE00000) [0000 0000 0001 1111 1111 1000 0000 0000]
            uint y = (packed & 0x1FF800) >> 11; // middle 10 bits --> 0x1FF800 is 1023(dec) << 11 --> [0000 0000 0001 1111 1111 1000 0000 0000]
            uint z = (packed & 2047); // last 11 bits, 2047 --> [0000 0000 0000 0000 0000 0111 1111 1111]

            float3 unpacked;
            // scaling equation: (((val - oldMin) / (oldMax - oldMin)) * (newMax - newMin)) + newMin;
            // since oldMin is zero, we can eliminate two subtractions
            // this gives: (((val - 0) / (oldMax - 0)) * (newMax - newMin)) + newMin;
            // which is the same as: ((val / oldMax) * (newMax - newMin)) + newMin;
            // we can optimize division since oldMax is 2048 for a 10 bit number and 1024 for a 10 bit number
            unpacked.x = (FastDivBy2048(x) * (max - min)) + min; // 11 bits
            unpacked.y = (FastDivBy1024(y) * (max - min)) + min; // 10 bits
            unpacked.z = (FastDivBy2048(z) * (max - min)) + min; // 11 bits
            return unpacked;
        }

        // given a packed value and the min/max values of val (before they were scaled)
        // returns a de-scaled float4 with 10 bit precisions for x,y,z and w will be -1 or 1
        public static float4 UnpackRgbaSignedToThree10BitFloats(float4 val, float min, float max)
        {
            uint packed = UnpackRgbaToUint(val);
            uint x = (packed >> 22); // first 10 bits
            uint y = (packed & 0x1FFC00) >> 10; // next 10 bits - 0x1FFC00 is (1023 dec << 10)
            uint z = (packed & 0x1FFC00) >> 2; // next 10 bits - 0x1FFC00 is (1023 dec << 10)
            uint w = packed & 4; // last two bits

            float4 unpacked;
            unpacked.x = (FastDivBy1024(x) * (max - min)) + min; // 10 bits
            unpacked.y = (FastDivBy1024(y) * (max - min)) + min; // 10 bits
            unpacked.z = (FastDivBy1024(z) * (max - min)) + min; // 10 bits
            // w is 0 or 1. convert it back to -1 or 1.
            unpacked.w = ScaleUnitIntervalToUnitRange((float)w);
            return unpacked; // the tangent
        }

        //####################################### PROTECTED ###############################################//

        const int DIV_11_MUL = 2097151; // uint-max / 2048, where uint-max is 4294967295
        const int DIV_10_MUL = 4194303; // uint-max / 1024, where uint-max is 4294967295
        const float DIV_11_MUL_INV = 0.00000047683738557690886350100684213965f; // 1 / div11mul
        const float DIV_10_MUL_INV = 0.00000023841863594499491333840211353352f; // 1 / div10mul

        // this is a way to divide by 2048, but giving a floating point result
        public static float FastDivBy2048(uint num) { return ((num * DIV_11_MUL) >> 11) * DIV_11_MUL_INV; }

        // this is a way to divide by 1024, but giving a floating point result
        public static float FastDivBy1024(uint num) { return ((num * DIV_10_MUL) >> 10) * DIV_10_MUL_INV; }
    }
} // namespace