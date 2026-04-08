using LFramework.Runtime.Settings;
using UnityEngine;

namespace LFramework.Editor.Window
{
    public readonly struct GameWindowSettingPageModel
    {
        public readonly Object Target;
        public readonly string Title;
        public readonly string Subtitle;

        public GameWindowSettingPageModel(Object target, string title, string subtitle)
        {
            Target = target;
            Title = title;
            Subtitle = subtitle;
        }
    }

    /// <summary>
    /// GameWindow 中可进入统一 Setting 页面壳的目标类型解析。
    /// </summary>
    public static class GameWindowSettingPageSupport
    {
        public static bool TryCreate(Object selected, out GameWindowSettingPageModel model)
        {
            switch (selected)
            {
                case ComponentSetting componentSetting:
                    model = new GameWindowSettingPageModel(
                        componentSetting,
                        GameWindowChrome.GetDisplayName(componentSetting.GetType().Name, "ComponentSetting"),
                        "A unified host for the selected setting asset while keeping the original custom editor behavior intact.");
                    return true;

                case SettingSelector selector:
                    model = new GameWindowSettingPageModel(
                        selector,
                        GameWindowChrome.GetDisplayName(selector.GetType().Name, "Selector"),
                        "A unified host for the selected selector asset while keeping the original custom editor behavior intact.");
                    return true;

                case BaseSetting baseSetting:
                    model = new GameWindowSettingPageModel(
                        baseSetting,
                        GameWindowChrome.GetDisplayName(baseSetting.GetType().Name, "Setting"),
                        "A unified host for the selected game setting asset while keeping the original custom editor behavior intact.");
                    return true;
            }

            model = default;
            return false;
        }
    }
}
