
using UnityEngine;
using UnityEngine.UI;

namespace Postica.BindingSystem.Accessors
{
    /// <summary>
    /// This class contains accessors for the UnityEngine.UI components.
    /// </summary>
    internal static class UnityEngineUIAccessors
    {
        [RegistersCustomAccessors]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void RegisterAccessors()
        {
            AccessorsFactory.RegisterAccessors(
                (typeof(Graphic), nameof(Graphic.color), new ObjectTypeAccessor<Graphic, Color>(g => g.color, (g, v) => g.canvasRenderer.SetColor(v))),
                (typeof(Image), nameof(Image.color), new ObjectTypeAccessor<Image, Color>(g => g.color, (g, v) => g.canvasRenderer.SetColor(v))),
                (typeof(RawImage), nameof(RawImage.color), new ObjectTypeAccessor<RawImage, Color>(g => g.color, (g, v) => g.canvasRenderer.SetColor(v)))
            );
        }
    }
}
