using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GoodMorningRainMeadow.Menu
{
    public class RemixMenu : OptionInterface
    {
        public RemixMenu()
        {
            messageSoundEnabled = config.Bind("MessageSound_Bool_Checkbox", true);
            
        }
        public readonly Configurable<bool> messageSoundEnabled;

        public override void Initialize()
        {
            var opTab1 = new OpTab(this, Menu.RemixMenu.Translate("Rain Meadow Chinese Input Settings"));
            Tabs = new[] { opTab1 }; // Add the tabs into your list of tabs. If there is only a single tab, it will not show the flap on the side because there is not need to.

            UIelement[] UIArrayElements = new UIelement[] // Labels in a fixed box size + alignment
            {
                new OpLabel(60, 503, "[" + Menu.RemixMenu.Translate("Message Sound") + "]"),
                new OpCheckBox(messageSoundEnabled, 30, 500),
            };
            opTab1.AddItems(UIArrayElements);
        }
    }
}
