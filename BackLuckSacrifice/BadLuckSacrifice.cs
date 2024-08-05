#region

using System;
using BepInEx;
using MonoMod.Cil;
using RoR2;
using PickupDropTable = On.RoR2.PickupDropTable;
using SacrificeArtifactManager = On.RoR2.Artifacts.SacrificeArtifactManager;

#endregion

namespace BadLuckSacrifice
{
    [BepInPlugin(Guid, ModName, Version)]
    public class BadLuckSacrifice : BaseUnityPlugin
    {
        private const string
            ModName = "BadLuckSacrifice",
            Author = "Yaya",
            Guid = "com." + Author + "." + "BadLuckSacrifice",
            Version = "1.0.0";

        private int _kills;

        public void OnEnable()
        {
            SacrificeArtifactManager.OnArtifactEnabled += Load;
            SacrificeArtifactManager.OnArtifactDisabled += UnLoad;
        }


        public void OnDisable()
        {
            SacrificeArtifactManager.OnArtifactEnabled -= Load;
            SacrificeArtifactManager.OnArtifactDisabled -= UnLoad;
        }


        private void Load(SacrificeArtifactManager.orig_OnArtifactEnabled orig, RunArtifactManager runArtifactManager,
            ArtifactDef artifactDef)
        {
            IL.RoR2.Artifacts.SacrificeArtifactManager.OnServerCharacterDeath += OnServerCharacterDeathIL;
            SacrificeArtifactManager.OnServerCharacterDeath += OnServerCharacterDeathON;
            PickupDropTable.GenerateDrop += OnGenerateDrop;

            orig(runArtifactManager, artifactDef);
        }

        private void UnLoad(SacrificeArtifactManager.orig_OnArtifactDisabled orig,
            RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            IL.RoR2.Artifacts.SacrificeArtifactManager.OnServerCharacterDeath -= OnServerCharacterDeathIL;
            SacrificeArtifactManager.OnServerCharacterDeath -= OnServerCharacterDeathON;
            PickupDropTable.GenerateDrop -= OnGenerateDrop;

            orig(runArtifactManager, artifactDef);
        }

        private PickupIndex OnGenerateDrop(PickupDropTable.orig_GenerateDrop orig, RoR2.PickupDropTable self,
            Xoroshiro128Plus rng)
        {
            _kills = 0;
            orig(self, rng);
        }

        private void OnServerCharacterDeathON(SacrificeArtifactManager.orig_OnServerCharacterDeath orig,
            DamageReport damagereport)
        {
            _kills++;
            orig(damagereport);
        }


        private void OnServerCharacterDeathIL(ILContext il)
        {
            var c = new ILCursor(il);

            //Change base drop chance
            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(5f)
            );
            c.EmitDelegate<Func<float, float>>(Calcul);

            c.GotoNext(
                x => x.MatchStloc(0) //Called after GetExpAdjustedDropChancePercent
            );
            c.EmitDelegate<Func<float, float>>(Calcul);
        }

        private float Calcul(float value)
        {
            return (float)((RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.swarmsArtifactDef)
                ? 0.5
                : 1) * (value * Math.Pow(0.002f * _kills, 2f)));
        }
    }
}