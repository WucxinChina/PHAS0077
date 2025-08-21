namespace Gley.UrbanSystem.Editor
{
    public readonly struct WindowProperties
    {
        public string NameSpace { get; }
        public string ClassName { get; }
        public string Title { get; }
        public string TutorialLink { get; }
        public bool ShowBack { get; }
        public bool ShowTitle { get; }
        public bool ShowTop { get; }
        public bool ShowScroll { get; }
        public bool ShowBottom { get; }
        public bool BlockClicks { get; }


        public WindowProperties(string nameSpace, string className, string title, bool showBack, bool showTitle, bool showTop, bool showScroll, bool showBottom, bool blockClicks, string tutorialLink)
        {
            NameSpace = nameSpace;
            ClassName = className;
            Title = title;
            ShowBack = showBack;
            ShowTitle = showTitle;
            ShowTop = showTop;
            ShowScroll = showScroll;
            ShowBottom = showBottom;
            BlockClicks = blockClicks;
            TutorialLink = tutorialLink;
        }
    }
}
