using System;
using Avalonia.Media;

namespace Jc.MediaImporter.Models;

public record NavigationMenuItem(Type ModelType, string Label, string Icon, Brush Background);