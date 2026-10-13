using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Votann40k;

public class KinLeagueDef : Def
{
    public string iconPath;

    public List<GeneDef> genes = new();

    public int displayOrder;

    public bool defaultLeague;

    [Unsaved(false)]
    private Texture2D cachedIcon;

    public Texture2D Icon
    {
        get
        {
            if (cachedIcon == null)
            {
                cachedIcon = iconPath.NullOrEmpty() ? BaseContent.BadTex : ContentFinder<Texture2D>.Get(iconPath);
            }

            return cachedIcon;
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
        {
            yield return error;
        }

        if (genes.NullOrEmpty())
        {
            yield return "no genes defined";
        }
    }
}
