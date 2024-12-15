using System;
using Avalonia.Media;
using Jc.MediaImporter.ViewModels;

namespace Jc.MediaImporter.Models;

public record NavigationMenuItem(Func<ViewModelBase> Factory, string Label, string Icon, Brush Background);