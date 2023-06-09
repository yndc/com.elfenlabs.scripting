using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Elfenlabs.Scripting
{
    public static class ScriptUtility
    {
        public static BlobAssetReference<Script> CreateTest()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<Script>();
            var scriptBuilder = new ByteCodeBuilder(Allocator.Temp);
            scriptBuilder.Yield((half)1f);
            scriptBuilder.Halt();
            scriptBuilder.Build(builder, ref root.Code);
            var result = builder.CreateBlobAssetReference<Script>(Allocator.Persistent);
            builder.Dispose();
            return result;
        }

        public static BlobAssetReference<Script> CreateReference(ByteCodeBuilder builder)
        {
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var root = ref blobBuilder.ConstructRoot<Script>();
            builder.Build(blobBuilder, ref root.Code);
            var result = blobBuilder.CreateBlobAssetReference<Script>(Allocator.Persistent);
            blobBuilder.Dispose();
            return result;
        }
    }
}