using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Presentation;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Platform;
using Dovus.Game.Config;
using Dovus.Game.Data;
using Dovus.Game.Hud;
using Dovus.Game.Skills.Boss;
using Dovus.Game.Skills.Closing;
using Dovus.Game.Skills.Flow;
using Dovus.Game.Skills.Passives;
using Dovus.Game.Skills.Hosts;
using Dovus.Game.Skills.Sync;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        internal MdCoreServicesHost _coreServicesHost;
        internal ClosingQueue _closingQueue;
        internal SlotPassiveRuntime _slotPassiveRuntime;
        BossDeathSequence _bossDeathSequence;
        SentenceSync _sentenceSync;

        internal void EnsureCoreServices()
        {
            if (_coreServicesHost != null)
                return;
            _coreServicesHost = new MdCoreServicesHost(this);
            _closingQueue = new ClosingQueue(_coreServicesHost, _pending);
            _slotPassiveRuntime = new SlotPassiveRuntime(_coreServicesHost);
            _bossDeathSequence = new BossDeathSequence(_coreServicesHost);
            _sentenceSync = new SentenceSync(_coreServicesHost);
        }

        internal List<PendingClosing> PendingList => _pending;
    }
}
