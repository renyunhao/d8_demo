// this class holds some "components" that represent material properties, which can be used
// to change per-instance properties of the playback shader.
//--------------------------------------------------------------------------------------------------//

using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;

namespace AnimCooker
{
    //   shift.x --> time (passed in every frame by animation system)
    //   shift.y --> global begin frame (passed in every frame by animation system)
    //   shift.z --> global end frame (passed in every frame by animation system)
    [MaterialProperty("_Shift", -1)]
    public struct MatPropShift : IComponentData
    {
        public float4 Shift;
    }
} // namespace