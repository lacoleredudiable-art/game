using Dovus.Core.Motion;
using Dovus.Game.Assets;
using System;
using UnityEngine;

namespace Dovus.Game.Skills.Motion
{
    public sealed partial class MotionTemplateDriver
    {
        IMotionTemplateRepository MotionRepo
        {
            get
            {
                if (_repository != null && _repository.TemplateCount > 0)
                    return _repository;
                if (_lazyCatalog != null && _lazyCatalog.TemplateCount > 0)
                    return _lazyCatalog;
                TextAsset asset = AssetLoader.Load<TextAsset>("ElementSystem/motion-templates", null);
                if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                    throw new InvalidOperationException("motion-templates.json missing/invalid");

                try
                {
                    _lazyCatalog = MotionTemplateCatalog.FromJson(asset.text);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("motion-templates.json missing/invalid", e);
                }
                return _lazyCatalog;
            }
        }

        MotionTemplateCatalog MotionCatalog => MotionRepo as MotionTemplateCatalog
            ?? throw new InvalidOperationException("motion-templates repository is not a MotionTemplateCatalog");
    }
}
