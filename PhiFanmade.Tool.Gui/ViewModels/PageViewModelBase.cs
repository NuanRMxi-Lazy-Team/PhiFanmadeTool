﻿using System.Collections.ObjectModel;

namespace PhiFanmade.Tool.Gui.ViewModels;

/// <summary>所有页面 ViewModel 的抽象基类，提供日志集合与工具方法。</summary>
public abstract partial class PageViewModelBase : ViewModelBase
{
    public abstract string PageTitle { get; }

    // read only
}
