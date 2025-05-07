using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace TogglAutomationApp;

public static class ToastNotification
{
    public static void Show(ToastElement[] elements)
    {
        if (elements.Length == 0)
            return;

        var builder = new ToastContentBuilder();
        foreach( var element in elements )
        {
            if(element.Type == "text")
            {
                if (element.Text == null) continue;
                builder.AddText(element.Text);
            }

            else if(element.Type == "image")
            {
                if (element.Url == null) continue;

                builder.AddHeroImage(new Uri(element.Url));
            }
        }
        builder.Show();
    }
}
