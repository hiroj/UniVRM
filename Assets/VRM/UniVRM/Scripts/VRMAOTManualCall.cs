using System;
using UniJSON;
using UniGLTF;
using System.Collections.Generic;


namespace VRM
{
    public static partial class VRMAOTCall
    {
        // Manually call the part where generics were used in the inner class
        static void AOTManualCall()
        {
            {
                UniJSON.JsonObjectValidator.GenericValidator<UnityEngine.Vector3>.ObjectValidator._CreateFieldValidator<float>(null);
            }
            {
                UniJSON.JsonObjectValidator.GenericSerializer<UnityEngine.Vector3>.Serializer._CreateFieldSerializer<float>(null);
            }
            {
                UniJSON.FormatterExtensionsSerializer.SerializeArray<float>(null, null);
                UniJSON.FormatterExtensionsSerializer.SerializeArray<int>(null, null);
            }
        }
    }
}