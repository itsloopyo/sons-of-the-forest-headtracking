// Reference stub for the one member of the game's own code this mod compiles against.
// Compiled into an assembly named Sons by stubs/Sons/Stubs.Sons.csproj, because that is
// where the shipped Il2CppInterop proxy declares TheForest.Utils.LocalPlayer and the
// compiler bakes that assembly identity into the reference.
//
// This is a declaration of an API shape, not Endnight's code. The signature was read off a
// shipped BepInEx/interop/Sons.dll with Cecil: LocalPlayer derives from MonoBehaviour and
// IsInWorld is a static get-only property, so the call site emits
// `call bool [Sons]TheForest.Utils.LocalPlayer::get_IsInWorld()`. Declaring it as a field
// instead would emit ldsfld and throw MissingFieldException in game.
//
// AssemblyVersion 0.0.0.0 matches what the interop assemblies carry.

using UnityEngine;

namespace TheForest.Utils
{
    public class LocalPlayer : MonoBehaviour
    {
        public static bool IsInWorld => default;
    }
}
