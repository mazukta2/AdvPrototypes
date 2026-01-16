
using UnityEngine;

namespace Postica.BindingSystem.Accessors
{
    /// <summary>
    /// This class contains accessors for the ParticleSystem component.
    /// </summary>
    internal static class ParticleSystemAccessors
    {
        [RegistersCustomAccessors]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void RegisterAccessors()
        {
            AccessorsFactory.RegisterAccessors(
                (typeof(ParticleSystem), nameof(ParticleSystem.main), Module(p => p.main)),
                (typeof(ParticleSystem), nameof(ParticleSystem.collision), Module(p => p.collision)),
                (typeof(ParticleSystem), nameof(ParticleSystem.emission), Module(p => p.emission)),
                (typeof(ParticleSystem), nameof(ParticleSystem.lights), Module(p => p.lights)),
                (typeof(ParticleSystem), nameof(ParticleSystem.noise), Module(p => p.noise)),
                (typeof(ParticleSystem), nameof(ParticleSystem.shape), Module(p => p.shape)),
                (typeof(ParticleSystem), nameof(ParticleSystem.trails), Module(p => p.trails)),
                (typeof(ParticleSystem), nameof(ParticleSystem.trigger), Module(p => p.trigger)),
                (typeof(ParticleSystem), nameof(ParticleSystem.customData), Module(p => p.customData)),
                (typeof(ParticleSystem), nameof(ParticleSystem.externalForces), Module(p => p.externalForces)),
                (typeof(ParticleSystem), nameof(ParticleSystem.inheritVelocity), Module(p => p.inheritVelocity)),
                (typeof(ParticleSystem), nameof(ParticleSystem.subEmitters), Module(p => p.subEmitters)),
                (typeof(ParticleSystem), nameof(ParticleSystem.colorBySpeed), Module(p => p.colorBySpeed)),
                (typeof(ParticleSystem), nameof(ParticleSystem.colorOverLifetime), Module(p => p.colorOverLifetime)),
                (typeof(ParticleSystem), nameof(ParticleSystem.forceOverLifetime), Module(p => p.forceOverLifetime)),
                (typeof(ParticleSystem), nameof(ParticleSystem.rotationBySpeed), Module(p => p.rotationBySpeed)),
                (typeof(ParticleSystem), nameof(ParticleSystem.rotationOverLifetime), Module(p => p.rotationOverLifetime)),
                (typeof(ParticleSystem), nameof(ParticleSystem.sizeBySpeed), Module(p => p.sizeBySpeed)),
                (typeof(ParticleSystem), nameof(ParticleSystem.sizeOverLifetime), Module(p => p.sizeOverLifetime)),
                (typeof(ParticleSystem), nameof(ParticleSystem.textureSheetAnimation), Module(p => p.textureSheetAnimation)),
                (typeof(ParticleSystem), nameof(ParticleSystem.lifetimeByEmitterSpeed), Module(p => p.lifetimeByEmitterSpeed)),
                (typeof(ParticleSystem), nameof(ParticleSystem.limitVelocityOverLifetime), Module(p => p.limitVelocityOverLifetime)),
                (typeof(ParticleSystem), nameof(ParticleSystem.velocityOverLifetime), Module(p => p.velocityOverLifetime)));
        }
        
        private static ObjectTypeAccessor<ParticleSystem, T> Module<T>(System.Func<ParticleSystem, T> getter)
        {
            return new ObjectTypeAccessor<ParticleSystem, T>(getter, (_, _) => { });
        }

    }
}
