// This is a collection of functions for decoding different bit depths stuffed into rgba buffers,
// and for accessing pixels in the position and normal textures.
//
// To understand how the pixels are stored, see example the diagram at https://gitlab.com/lclemens/animationcooker/ .
// 
// There are really two ways of accessing the pixels, which can be determined via the flag ENABLE_VA_RGBA10BIT.
//   1) One way is via R11B10G11, which is essentially 11 bits for X and Y and 10 bits for Y.
//   2) The other way is just normally, where unity's encoding schemes can be used.
// 
// Given variables:
//   vtxIdx - provided by shader main function, for each vertex
//   frameRate - saved in the animation table and material (_Stat1.w)
//   vertCount - saved in the animation table and material (_Stat3.x)
//   tex - saved per material (_PosMap, _NmlMap, _TanMap)
//   min - saved per material (_Stat1, _Stat2)
//   max - saved per material (_Stat1, _Stat2)
//   pow2 - saved per material (_Stat1.x)
//   ts - provided by material automatically (_PosMap_TexelSize, _NmlMap_TexelSize, _TanMap_TexelSize)
//   skinIndex - saved per material (_Stat3.y) - not really used in this shader
//   time - incremented every frame by the animation system (CPU) and passed to the shader every frame (_Shift.x)
//   globalBeginFrame - saved in the animation table, passed to shader every frame by animationsystem (_Shift.y)
//   globalEndFrame - saved in the animation table, passed to shader every frame by animationsystem (_Shift.z)
//=================================================================================================

#pragma once

//#define IF(a, b, c) lerp(b, c, step((fixed) (a), 0));

#define bitDec float4(1.0, 255, 65025, 16581375)

#define div11mul 2097151 // uint-max / 2048, where uint-max is 4294967295
#define div10mul 4194303 // uint-max / 1024, where uint-max is 4294967295
#define div11mulInv 0.00000047683738557690886350100684213965 // 1 / div11mul
#define div10mulInv 0.00000023841863594499491333840211353352 // 1 / div10mul

// this is a way to divide by 2048, but giving a floating point result
#define FastDivBy2048(num) ((num * div11mul) >> 11) * div11mulInv

// this is a way to divide by 1024, but giving a floating point result
#define FastDivBy1024(num) ((num * div10mul) >> 10) * div10mulInv

struct PixelInfo
{
    float3 position;
    float3 normal;
    float4 tangent;
};

// this is a replacement for the old 'UnityObjectToWorldNormal()'
#define ObjectToWorldNormal(normal) mul(GetObjectToWorldMatrix(), float4(normal, 1))

// this is a replacement for the old 'UnityObjectToClipPos()'
#define ObjectToClipPos(pos) mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4(pos.x, pos.y, pos.z, 1)))
//#define ObjectToClipPos(pos) mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, pos))

#define ScaleUnitIntervalToUnitRange(val) (val * 2) - 1
#define ScaleToUnitRange(val, oldMax) ((val / oldMax) * 2) - 1
#define Scale(val, oldMin, oldMax, newMin, newMax) (((val - oldMin) / (oldMax - oldMin)) * (newMax - newMin)) + newMin
#define ScaleFromUnitInterval(val, newMin, newMax) (val * (newMax - newMin)) + newMin

uint UnpackRgbaToUint(float4 v)
{
    //return ((uint)(v.x * 255) << 24) | ((uint)(v.y * 255) << 16) | ((uint)(v.z * 255) << 8) | ((uint)(v.w * 255));
    return (((uint) round(v.x * 255)) << 24) | (((uint) round(v.y * 255)) << 16) | (((uint) round(v.z * 255)) << 8) | ((uint) round(v.w * 255));
}

// given a packed value and the min and max values of val (before they were scaled)
// returns a de-scaled float3 with precision such that z and x are 11 bits, and y is 10 bits.
// 00 01 02 03 04 05 06 07 08 09 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31
// |--------------11 bits (x)-----| |-----------10 bits (y)-----| |----------11 bits (z) --------|
float3 Unpack10BitToFloat3(float4 val, float min, float max)
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
float4 Unpack10BitToFloat4(float4 val, float min, float max)
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

// This macro makes it easier to lookup the specified value of a pixel
// xx is the x coordinate, yy is the y coordinate, and ts is the texel size (such as _PosMap_TexelSize)
// I had to use xx and yy instead of x and y because the macro compiler gets confused with the x and y values in ts.x and ts.y
// for the float passed into tex2Dlod(): x and y are the coordinates, z is the LOD, and w is the offset.
// The return value is a float4 corresponding to RGBA values, and it is NOT decoded.
#define GetPixel(tex, xx, yy, ts) (tex2Dlod(tex, float4((xx + 0.5) * ts.x, (yy + 0.5) * ts.y, 0, 0)))

// fetches the value at x, y and returns a float3.
// x and z will have a precision of 11 bits for x, while y's precision will be 10 bits.
// you must specify a min and max value that was used to pack it
#define Lookup10BitFloat3(tex, x, y, ts, min, max) Unpack10BitToFloat3(GetPixel(tex, x, y, ts), min, max)

// fetches teh value at x,y and returns a float4 where x,y,z are 10 bit ushort values and w is -1 or 1 for sign
// (mainly used for tangents)
#define Lookup10BitFloat4(tex, x, y, ts, min, max) Unpack10BitToFloat4(GetPixel(tex, x, y, ts), min, max)

// This function takes the specified shift and ranges and coverts it to xy and returns the decoded pixel value.
// If the ENABLE_VA_RGBA10BIT toggle is enabled, the pixel will be decoded as an R11G10B11 value (X11, Y10, Z11).
// Otherwise, the value will just be decoded however it was encoded (depends on the setting in the texture import).
float3 LookupPixel(sampler2D tex, float4 ts, uint idx, float pow2, float min, float max)
{
    //uint idx = shift + vtxIdx;
    float y = idx >> (uint) pow2; // shift by log2(width) is the same as dividing by width as long as width is a power of 2
    float x = idx - (ts.z * y); // ts.z is texture width
    #if defined(ENABLE_VA_RGBA10BIT)
        return Lookup10BitFloat3(tex, x, y, ts, min, max);
    #else
        float4 ret = ScaleFromUnitInterval(GetPixel(tex, x, y, ts), min, max);
        return float3(ret.x, ret.y, ret.z);
    #endif
}

// This is similar to LookupPixel(), except it fetches the pixel as a float4 with a signed bit.
// This is mainly only used for the tangent texture.
float4 LookupPixelSign(sampler2D tex, float4 ts, uint idx, float pow2, float min, float max)
{
    //uint idx = shift + vtxIdx; 
    float y = idx >> (uint) pow2; // stat1.x is pow2
    float x = idx - (ts.z * y); // ts.z is texture width
    #if defined(ENABLE_VA_RGBA10BIT)
        return Lookup10BitFloat4(tex, x, y, ts, min, max);
    #else
        return ScaleFromUnitInterval(GetPixel(tex, x, y, ts), min, max);
    #endif
}

// This is just a macro simplifcation for InterpVal with the encoded position map (it assumes all the variables that start with _ such as _Shift are defined)
#define InterpPos(vtxIdx) InterpVal(_PosMap, _PosMap_TexelSize, vtxIdx, _Stat1.x, _Stat1.y, _Stat1.z, _Stat1.w, _Stat3.x, _Shift.x, _Shift.y, _Shift.z)

// This is just a macro simplifcation for InterpVal with the encoded normal map (it assumes all the variables that start with _ such as _Shift are defined)
#define InterpNml(vtxIdx) InterpVal(_NmlMap, _NmlMap_TexelSize, vtxIdx, _Stat1.x, _Stat2.x, _Stat2.y, _Stat1.w, _Stat3.x, _Shift.x, _Shift.y, _Shift.z)

// This is just a macro simplifcation for InterpVal with the encoded tangent map (it assumes all the variables that start with _ such as _Shift are defined)
#define InterpTan(vtxIdx) InterpValWithSign(_TanMap, _TanMap_TexelSize, vtxIdx, _Stat2.z, _Stat2.w, _Stat1.z, _Stat1.w, _Stat3.x, _Shift.x, _Shift.y, _Shift.z)

#define GlobalFrameToVertex(f, vcnt, vidx) (f * vcnt) + vidx

// tex --> the texture
// ts --> texture width/height info passed in by the shader
// vtxIdx --> vertex id (passed in by shader)
// pow2 --> the power-of-two used for width log2(width)
// min --> the minimum value in the range
// max --> the maxium value in the range
// frameRate --> the frame rate that the current skin/material was originally sampled at.
// vertCount --> the total number of vertexes the model has
// time --> the current time (updated every frame by animation system)
// globalBeginFrame --> global begin frame for the current clip (updated every frame by animation system)
// globalEndFrame --> global end frame for the current clip (updated every frame by animation system)
float3 InterpVal(sampler2D tex, float4 ts, uint vtxIdx, float pow2, float min, float max, float frameRate, int vertCount, float time, int globalBeginFrame, int globalEndFrame)
{
    // Convert the time to a current frame (which will be local to the current clip)
    float curOffset = time * frameRate; // local offset of frame for this clip
    int curFrame = floor(curOffset); // current local frame number for this clip
    
    // get the global frame number by adding current frame to the global begin frame
    int curGlobalFrame = curFrame + globalBeginFrame;
    
    // Get the global index for the current frame and vertex.
    // multiplying by vertCount converts it to an index. adding vtxIdx accounts for current index.
    int curIdx = GlobalFrameToVertex(curGlobalFrame, vertCount, vtxIdx);
    
    // If this is the last frame, then just use curIdx, 
    // otherwise use the same vertex of the next frame (by adding vertCount)
    int beginIdx = GlobalFrameToVertex(globalBeginFrame, vertCount, vtxIdx); // lets it wrap to beginning.
    int nextIdx = (curGlobalFrame >= globalEndFrame) ? beginIdx : curIdx + vertCount; // adding vertCount skips to next index
    
    // decode the 11/10/11 bit RGBA values for the current and next pixel
    float3 curVal = LookupPixel(tex, ts, curIdx, pow2, min, max);
    float3 nextVal = LookupPixel(tex, ts, nextIdx, pow2, min, max);
    
    // the lerp amount should be a number between 0 and 1 and a function of the current time in between cur and next frame.
    // note - if curIdx and nextIdx are the same (last frame), then it won't matter what the lerp value is.
    float lerpAmt = curOffset - (float)curFrame;
    return lerp(curVal, nextVal, lerpAmt);
}


// same as InterpVal, except it uses float4 and a sign (for tangent textures)
float4 InterpValWithSign(sampler2D tex, float4 ts, uint vtxIdx, float pow2, float min, float max, float frameRate, int vertCount, float time, float globalBeginFrame, float globalEndFrame)
{
    int curFrame = floor(time * frameRate);
    int curIdx = ((curFrame + globalBeginFrame) * vertCount) + vtxIdx;
    int nextIdx = (globalBeginFrame + curFrame) >= globalEndFrame ? curIdx : curIdx + vertCount;
    float4 curVal = LookupPixelSign(tex, ts, curIdx, pow2, min, max);
    float4 nextVal = LookupPixelSign(tex, ts, nextIdx, pow2, min, max);
    float lerpAmt = (frameRate * time) - curFrame;
    return lerp(curVal, nextVal, lerpAmt);
}