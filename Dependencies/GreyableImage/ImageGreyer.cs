//Copyright (c) 2008 Alexey Potapov

//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights to 
//use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies 
//of the Software, and to permit persons to whom the Software is furnished to do 
//so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all 
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS 
//FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
//COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER 
//IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION 
//WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RoliSoft.TVShowTracker.Dependencies.GreyableImage
{
  /// <summary>
  /// ImageGreyer class exposing attachable dependency property that when attached to an Image
  /// and set to true will couse that Image to turn greyscale when IsEnabled is set to false.
  /// 
  /// This is intended to be used for images in toolbars, menus or buttons where ability of an icon to 
  /// grey itself out when disabled is essential.
  /// 
  /// This class implements the attached property trick brilliantly described by Dan Crevier in his blog:
  ///    http://blogs.msdn.com/dancre/archive/2006/03/04/543854.aspx
  /// <remarks>
  /// 1) Greyscale image is created using FormatConvertedBitmap class. Unfortunately when converting the
  ///    image to greyscale this class does not preserve transparency information. To overcome that, there is 
  ///    an opacity mask created from original image that is applied to greyscale image in order to preserve
  ///    transparency information. Because of that if an OpacityMask is applied to original image that mask 
  ///    has to be combined with that special opacity mask of greyscale image in order to make a proper 
  ///    greyscale image look. If you know how to combine two opacity masks please let me know.
  /// 2) When specifying source Uri from XAML try to use Absolute Uri otherwise the greyscale image
  ///    may not be created in some scenarious. There is GetAbsoluteUri() method aiming to improve the situation 
  ///    by trying to generate an absolute Uri from given source, but I cannot guarantee it will work in all 
  ///    possible scenarious.
  /// 3) In case the greyscaled version cannot be created for whatever reason the original image with 
  ///    60% opacity (i.e. dull colours) will be used instead.
  /// 4) Changing Source from code will take precedence over Style triggers. Source set through triggers 
  ///    will be ignored once it was set from code. This is not the fault of the control, but is the way 
  ///    WPF works: http://msdn.microsoft.com/en-us/library/ms743230%28classic%29.aspx.
  /// 5) Supports DrawingImage as a source, thanks to Morten Schou.
  /// 6) Image Source or OpacityMask bindings now work when greyability effect is used.
  /// 7) Supports BitmapSource/InteropBitmap as a source, thanks to Patrick van der Velde.
  /// </remarks>
  /// </summary>
  public class ImageGreyer
  {
    #region Fields

    // image this effect is attached to
    private Image _image;

    // these are holding references to original and greyscale ImageSources
    private ImageSource _sourceColour, _sourceGreyscale;

    // these are holding original and greyscale opacity masks
    private Brush _opacityMaskColour, _opacityMaskGreyscale;

    // bindings that were set on Source and OpacityMask properties of an image
    private Binding _sourceBinding, _opacityMaskBinding;

    #endregion // Fields

    #region Attachable Properties

    #region IsGreyable

    /// <summary>
    /// Attach this property to standart WPF image and if set to true will make that image greyable
    /// </summary>
    public static DependencyProperty IsGreyableProperty =
      DependencyProperty.RegisterAttached("IsGreyable", typeof(bool), typeof(ImageGreyer),
                                          new PropertyMetadata(false, new PropertyChangedCallback(OnChangedIsGreyable)));

    /// <summary>
    /// Attached property accessors
    /// </summary>
    public static bool GetIsGreyable(DependencyObject sender)
    {
      return (bool)sender.GetValue(IsGreyableProperty);
    }
    public static void SetIsGreyable(DependencyObject sender, bool isGreyable)
    {
      sender.SetValue(IsGreyableProperty, isGreyable);
    }

    /// <summary>
    /// Callback when the IsGreyable property is set or changed.
    /// </summary>
    private static void OnChangedIsGreyable(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
      Image image = dependencyObject as Image;
      if (null != image)
      {
        if ((bool)e.NewValue)
        {
          // turn greyability effect on if it is not turned on yet
          if (image.ReadLocalValue(GreyabilityEffectProperty) == DependencyProperty.UnsetValue)
          {
            ImageGreyer greyability = new ImageGreyer(image);
            image.SetValue(GreyabilityEffectProperty, greyability);
          }
        }
        else
        {
          // remove greyability effect
          if (image.ReadLocalValue(GreyabilityEffectProperty) != DependencyProperty.UnsetValue)
          {
            ImageGreyer greyability = (ImageGreyer)image.ReadLocalValue(GreyabilityEffectProperty);
            greyability.Detach();
            image.SetValue(GreyabilityEffectProperty, DependencyProperty.UnsetValue);
          }
        }
      }
    }

    #endregion // IsGreyable

    #region GreyabilityEffect

    /// <summary>
    /// attachable dependency property to be set on image to store reference to ourselves - private, used by this class only
    /// </summary>
    public static DependencyProperty GreyabilityEffectProperty =
      DependencyProperty.RegisterAttached("GreyabilityEffect", typeof(ImageGreyer), typeof(ImageGreyer));

    #endregion // GreyabilityEffect

    #region Source

    /// <summary>
    /// Attachable DependencyProperty used as a backing store for Source property.
    /// </summary>
    public static readonly DependencyProperty SourceProperty =
      DependencyProperty.RegisterAttached("Source", typeof(ImageSource), typeof(ImageGreyer),
                                          new PropertyMetadata(OnChangedSource));

    /// <summary>
    /// Callback when the Source property is set or changed.
    /// </summary>
    private static void OnChangedSource(DependencyObject o, DependencyPropertyChangedEventArgs args)
    {
      Image img = o as Image;
      if (null != img)
        img.Source = args.NewValue as ImageSource;
    }

    #endregion // Source

    #region OpacityMask

    /// <summary>
    /// Attachable DependencyProperty used as a backing store for OpacityMask property.
    /// </summary>
    public static readonly DependencyProperty OpacityMaskProperty =
      DependencyProperty.RegisterAttached("OpacityMask", typeof(Brush), typeof(ImageGreyer),
                                          new PropertyMetadata(OnChangedOpacityMask));

    /// <summary>
    /// Callback when the OpacityMask property is set or changed.
    /// </summary>
    private static void OnChangedOpacityMask(DependencyObject o, DependencyPropertyChangedEventArgs args)
    {
      Image img = o as Image;
      if (null != img)
        img.OpacityMask = args.NewValue as Brush;
    }

    #endregion // OpacityMask

    #endregion // Attachable Properties

    #region Constructor

    public ImageGreyer(Image image)
    {
      _image = image;

      // If the image is not initialized yet, the Source is not set and SetSource will return without caching
      // the sources. Still change notification for Source property will not be fired if the Source was set 
      // from XAML e.g. <Image Source="image.png"/>. In this case we have to wait until the Image is initialized
      // which will mean that the Source is set (if it is supposed to be set from XAML) and we can cache it.
      // otherwise we just call SetSources caching all requireв data.
      if (!_image.IsInitialized)
      {
        // delay attaching to an image untill it is ready
        _image.Initialized += OnChangedImageInitialized;
      }
      else
      {
        // attach greyability effect to an image
        Attach();
      }
    }

    #endregion // Constructor

    #region Event handlers

    /// <summary>
    /// Called when IsInitialized property of the Image is set to true
    /// </summary>
    private void OnChangedImageInitialized(object sender, EventArgs e)
    {
      // image is ready for attaching greyability effect
      Attach();
    }

    /// <summary>
    /// Called when IsEnabled property of the Image is changed
    /// </summary>
    private void OnChangedImageIsEnabled(object sender, DependencyPropertyChangedEventArgs e)
    {
      UpdateImage();
    }

    /// <summary>
    /// Called when Source property of the Image is changed
    /// </summary>
    private void OnChangedImageSource(object sender, EventArgs e)
    {
      Image image = sender as Image;

      // only recache Source if it's a new one
      if (null != image &&
          !object.ReferenceEquals(image.Source, _sourceColour) &&
          !object.ReferenceEquals(image.Source, _sourceGreyscale))  
      {
        SetSources();
        SetGreyscaleOpacityMask();

        // have to asynchronously invoke UpdateImage because it changes the Source property 
        // of an image, but we cannot change it from within its change notification handler.
        image.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(UpdateImage));
      }
    }

    /// <summary>
    /// Called when OpacityMask property of the Image is changed
    /// </summary>
    private void OnChangedImageOpacityMask(object sender, EventArgs e)
    {
      Image image = sender as Image;

      // only recache opacityMask if it's a new one
      if (null != image &&
          !object.ReferenceEquals(image.OpacityMask, _opacityMaskColour) &&
          !object.ReferenceEquals(image.OpacityMask, _opacityMaskGreyscale))
      {
        _opacityMaskColour = _image.OpacityMask;

        // have to asynchronously invoke UpdateImage because it changes the OpacityMask property 
        // of an image, but we cannot change it from within its change notification handler.
        image.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(UpdateImage));
      }
    }

    #endregion Event handlers

    #region Helper methods

    #region Attach

    /// <summary>
    /// Attaching greyability effect to an Image
    /// </summary>
    private void Attach()
    {
      // First we steal the bindings that might be set on the image
      StealBinding();

      // then we cache original and greyscale Sources ...
      SetSources();

      // ... and OpacityMasks
      _opacityMaskColour = _image.OpacityMask;
      SetGreyscaleOpacityMask();

      // now if the image is disabled we need to grey it out now
      UpdateImage();

      // set event handlers
      _image.IsEnabledChanged += OnChangedImageIsEnabled;

      // there is no change notification event for OpacityMask dependency property 
      // in Image class but we can use property descriptor to add value changed callback
      DependencyPropertyDescriptor dpDescriptor = DependencyPropertyDescriptor.FromProperty(Image.OpacityMaskProperty, typeof(Image));
      dpDescriptor.AddValueChanged(_image, OnChangedImageOpacityMask);

      // there is no change notification for Source dependency property
      // in Image class but we can use property descriptor to add value changed callback
      dpDescriptor = DependencyPropertyDescriptor.FromProperty(Image.SourceProperty, typeof(Image));
      dpDescriptor.AddValueChanged(_image, OnChangedImageSource);
    }

    #endregion // Attach

    #region Detach

    /// <summary>
    /// Detaches this effect from the image, 
    /// </summary>
    private void Detach()
    {
      if (_image != null)
      {
        // remove all event handlers first...
        _image.IsEnabledChanged -= OnChangedImageIsEnabled;

        // there is no special change notification event for OpacityMask dependency property in Image class
        // but we can use property descriptor to remove value changed callback
        DependencyPropertyDescriptor dpDescriptor = DependencyPropertyDescriptor.FromProperty(Image.OpacityMaskProperty, typeof(Image));
        dpDescriptor.RemoveValueChanged(_image, OnChangedImageOpacityMask);

        // there is no change notification event for Source dependency property 
        // in Image class but we can use property descriptor to add value changed callback
        dpDescriptor = DependencyPropertyDescriptor.FromProperty(Image.SourceProperty, typeof(Image));
        dpDescriptor.RemoveValueChanged(_image, OnChangedImageSource);

        // in case the image is disabled we have to change the Source and OpacityMask 
        // properties back to the original values
        if (null != _sourceBinding)
          _image.SetBinding(Image.SourceProperty, _sourceBinding);
        else
          _image.Source = _sourceColour;

        if (null != _opacityMaskBinding)
          _image.SetBinding(Image.OpacityMaskProperty, _opacityMaskBinding);
        else
          _image.OpacityMask = _opacityMaskColour;

        // now release all the references we hold
        _image = null;
        _opacityMaskColour = _opacityMaskGreyscale = null;
        _sourceColour = _sourceGreyscale = null;
      }
    }

    #endregion // Detach

    #region StealBinding

    /// <summary>
    /// Checks if Source and OpacityMask properties of an image are databoud
    /// and if so caches and clears these bindings. Then it clones these 
    /// bindings and sets them on an image to attached Source and OpacityMask 
    /// properties registered for this class.
    /// This allows to keep the binding working while the Source and OpacityMask
    /// properties of an image are been set directly from code when image is 
    /// enabled or disabled.
    /// </summary>
    private void StealBinding()
    {
      if (BindingOperations.IsDataBound(_image, Image.SourceProperty))
      {
        _sourceBinding = BindingOperations.GetBinding(_image, Image.SourceProperty);
        BindingOperations.ClearBinding(_image, Image.SourceProperty);

        Binding b = CloneBinding(_sourceBinding);
        b.Mode = BindingMode.OneWay;
        _image.SetBinding(SourceProperty, b);
      }

      if (BindingOperations.IsDataBound(_image, Image.OpacityMaskProperty))
      {
        _opacityMaskBinding = BindingOperations.GetBinding(_image, Image.OpacityMaskProperty);
        BindingOperations.ClearBinding(_image, Image.OpacityMaskProperty);

        Binding b = CloneBinding(_opacityMaskBinding);
        b.Mode = BindingMode.OneWay;
        _image.SetBinding(OpacityMaskProperty, b);
      }
    }

    #endregion // StealBinding

    #region SetSources

    /// <summary>
    /// Cashes original ImageSource, creates and caches greyscale ImageSource and greyscale opacity mask
    /// </summary>
    private void SetSources()
    {
      if (null == _image.Source)
        return;

      // in case greyscale image cannot be created set greyscale source to original Source first
      _sourceGreyscale = _sourceColour = _image.Source;

      try
      {
        BitmapSource colourBitmap;

        if (_sourceColour is BitmapSource)
          colourBitmap = _sourceColour as BitmapSource;
        else if (_sourceColour is DrawingImage)
        {
          // support for DrawingImage as a source - thanks to Morten Schou who provided this code
          colourBitmap = new RenderTargetBitmap((int)_sourceColour.Width,
                                                (int)_sourceColour.Height,
                                                96, 96,
                                                PixelFormats.Default);
          DrawingVisual drawingVisual = new DrawingVisual();
          DrawingContext drawingDC = drawingVisual.RenderOpen();

          drawingDC.DrawImage(_sourceColour,
                              new Rect(new Size(_sourceColour.Height,
                                                _sourceColour.Width)));
          drawingDC.Close();
          (colourBitmap as RenderTargetBitmap).Render(drawingVisual);
        }
        else
        {
          // get the string Uri for the original image source first
          String stringUri = TypeDescriptor.GetConverter(_sourceColour).ConvertTo(_sourceColour, typeof(string)) as string;

          // Create colour BitmapImage using an absolute Uri (generated from stringUri)
          colourBitmap = new BitmapImage(GetAbsoluteUri(stringUri));
        }

        // create and cache greyscale ImageSource
        _sourceGreyscale = new FormatConvertedBitmap(colourBitmap, PixelFormats.Gray8, null, 0);
      }
      catch (Exception e)
      {
        System.Diagnostics.Debug.Fail("The Image used cannot be greyed out.",
                                      "Make sure absolute Uri is used, relative Uri may sometimes resolve incorrectly.\n\nException: " + e.Message);
      }
    }

    #endregion // SetSources

    #region SetGreyscaleOpacityMask

    /// <summary>
    /// Creates and caches greyscale Image opacity mask.
    /// </summary>
    private void SetGreyscaleOpacityMask()
    {
      // create Opacity Mask for greyscale image as FormatConvertedBitmap used to 
      // create greyscale image does not preserve transparency info.
      _opacityMaskGreyscale = new ImageBrush(_sourceColour);
      _opacityMaskGreyscale.Opacity = 0.6;
    }

    #endregion // SetGreyscaleOpacityMask

    #region UpdateImage

    /// <summary>
    /// Sets image source and opacity mask from cache.
    /// </summary>
    public void UpdateImage()
    {
      if (_image.IsEnabled)
      {
        // change Source and OpacityMask of an image back to original values
        _image.Source = _sourceColour;
        _image.OpacityMask = _opacityMaskColour;
      }
      else
      {
        // change Source and OpacityMask of an image to values generated for greyscale version
        _image.Source = _sourceGreyscale;
        _image.OpacityMask = _opacityMaskGreyscale;
      }
    }

    #endregion // UpdateImage

    #region GetAbsoluteUri

    /// <summary>
    /// Creates and returns an absolute Uri using the path provided.
    /// Throws UriFormatException if an absolute URI cannot be created.
    /// </summary>
    /// <param name="stringUri">string uri</param>
    /// <returns>an absolute URI based on string URI provided</returns>
    /// <exception cref="UriFormatException - thrown when absolute Uri cannot be created from provided stringUri."/>
    /// <exception cref="ArgumentNullException - thrown when stringUri is null."/>
    private Uri GetAbsoluteUri(String stringUri)
    {
      Uri uri = null;

      // try to resolve it as an absolute Uri 
      // if uri is relative its likely to point in a wrong direction
      if (!Uri.TryCreate(stringUri, UriKind.Absolute, out uri))
      {
        // it seems that the Uri is relative, at this stage we can only assume that
        // the image requested is in the same assembly as this oblect,
        // so we modify the string Uri to make it absolute ...
        stringUri = "pack://application:,,,/" + stringUri.TrimStart(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

        // ... and try to resolve again
        // at this stage if it doesn't resolve the UriFormatException is thrown
        uri = new Uri(stringUri);
      }

      return uri;
    }

    #endregion // GetAbsoluteUri

    #region CloneBinding

    /// <summary>
    /// Clones Binding specified by the argument.
    /// </summary>
    /// <param name="original">Binding to be cloned.</param>
    /// <returns>Clone of the binding specified by the argument.</returns>
    private Binding CloneBinding(Binding original)
    {
      Binding clone = new Binding();

      clone.Path = original.Path;
      if (null != original.Source)
        clone.Source = original.Source;
      else if (null != original.RelativeSource)
        clone.RelativeSource = original.RelativeSource;
      else if (null != original.ElementName)
        clone.ElementName = original.ElementName;

      clone.Mode = original.Mode;
      clone.TargetNullValue = original.TargetNullValue;
      clone.UpdateSourceTrigger = original.UpdateSourceTrigger;

      clone.ValidatesOnDataErrors = original.ValidatesOnDataErrors;
      clone.ValidatesOnExceptions = original.ValidatesOnExceptions;
      foreach (var rule in original.ValidationRules)
        clone.ValidationRules.Add(rule);

      clone.FallbackValue = original.FallbackValue;

      clone.Converter = original.Converter;
      clone.ConverterCulture = original.ConverterCulture;
      clone.ConverterParameter = original.ConverterParameter;

      return clone;
    }

    #endregion // CloneBinding

    #endregion // Helper methods
  }
}

