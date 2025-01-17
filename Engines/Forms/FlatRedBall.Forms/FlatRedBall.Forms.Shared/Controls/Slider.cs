﻿using FlatRedBall.Forms.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using Gum.Wireframe;
using RenderingLibrary;
using Microsoft.Xna.Framework.Input;
using FlatRedBall.Input;

namespace FlatRedBall.Forms.Controls;

public class Slider : RangeBase, IInputReceiver
{
    #region Fields/Properties

    public double TicksFrequency { get; set; } = 1;

    public bool IsSnapToTickEnabled { get; set; } = false;

    public bool IsMoveToPointEnabled { get; set; }

    public bool IsThumbGrabbed => GuiManager.Cursor.WindowPushed == this.thumb?.Visual;

    public List<Keys> IgnoredKeys => throw new NotImplementedException();

    public bool TakingInput => throw new NotImplementedException();

    public IInputReceiver NextInTabSequence { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    double ValueOnThumbPush;

    #endregion

    #region Events

    public event Action<IInputReceiver> FocusUpdate;

    public event Action<Xbox360GamePad.Button> ControllerButtonPushed;
    public event Action<int> GenericGamepadButtonPushed;

    #endregion

    #region Initialize

    public Slider() : base()
    {
        Initialize();
    }

    public Slider(GraphicalUiElement visual) : base(visual)
    {
        Initialize();
    }

    private void Initialize()
    {
        Minimum = 0;
        Maximum = 100;
        LargeChange = 25;
        SmallChange = 5;

        // by default sliders use left/right to change the slider value
        this.IsUsingLeftAndRightGamepadDirectionsForNavigation = false;
    }

    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();

        Track.Push += HandleTrackPush;
        base.thumb.Visual.RemovedAsPushedWindow += HandleRemovedAsPushedWindow;
        UpdateState();
    }

    protected override void ReactToVisualRemoved()
    {
        base.ReactToVisualRemoved();

        Track.Push -= HandleTrackPush;

    }

    #endregion

    #region Event Handlers
    protected override void HandleThumbPush(object sender, EventArgs e)
    {
        var leftOfThumb = this.thumb.ActualX;

        if(this.thumb.Visual.XOrigin == RenderingLibrary.Graphics.HorizontalAlignment.Center)
        {
            leftOfThumb += this.thumb.ActualWidth / 2.0f;
        }
        else if(this.thumb.Visual.XOrigin == RenderingLibrary.Graphics.HorizontalAlignment.Right)
        {
            leftOfThumb += this.thumb.ActualWidth;
        }
        var cursorScreen = GuiManager.Cursor.GumX();

        cursorGrabOffsetRelativeToThumb = cursorScreen - leftOfThumb;

        ValueOnThumbPush = Value;
    }

    private void HandleRemovedAsPushedWindow(IWindow window)
    {
        if(ValueOnThumbPush != Value)
        {
            RaiseValueChangeCompleted();
        }
    }

    protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
    {
        base.OnMinimumChanged(oldMinimum, newMinimum);

        if(Visual != null)
        {
            UpdateThumbPositionAccordingToValue();
        }
    }

    private void HandleTrackPush(IWindow window)
    {
        var valueBefore = Value;
        if(IsMoveToPointEnabled)
        {
            var left = Track.GetAbsoluteX();
            var right = Track.GetAbsoluteX() + Track.GetAbsoluteWidth();

            var screenX = GuiManager.Cursor.GumX();

            var ratio = (screenX - left) / (right - left);

            ratio = System.Math.Max(0, ratio);
            ratio = System.Math.Min(1, ratio);

            var value = Minimum + (Maximum - Minimum) * ratio;

            ApplyValueConsideringSnapToTicks(value);
        }
        else
        {
            double newValue;

            var gumX = GuiManager.Cursor.GumX();
            if (gumX < thumb.ActualX)
            {
                newValue = Value - LargeChange;
                ApplyValueConsideringSnapToTicks(newValue);
            }
            else if (gumX > thumb.ActualX + thumb.ActualWidth)
            {
                newValue = Value + LargeChange;

                ApplyValueConsideringSnapToTicks(newValue);
            }
        }

        if(valueBefore != Value)
        {
            RaiseValueChangedByUi();
        }
    }

    protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
    {
        base.OnMaximumChanged(oldMaximum, newMaximum);

        if (Visual != null)
        {
            UpdateThumbPositionAccordingToValue();
        }
    }

    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);

        if (Visual != null)
        {
            UpdateThumbPositionAccordingToValue();
        }
    }

    #endregion

    private double ApplyValueConsideringSnapToTicks(double newValue)
    {
        var originalValue = newValue;

        if (IsSnapToTickEnabled)
        {
            newValue = Math.MathFunctions.RoundDouble(newValue, TicksFrequency, Minimum);

            var range = Maximum - Minimum;
            var lastTick = ((int)((Maximum - Minimum) / TicksFrequency)) * TicksFrequency;

            if(originalValue > lastTick)
            {
                // see if we snap to end or not...
                var distanceFromLastTick = System.Math.Abs(originalValue - lastTick);
                var distanceFromMax = System.Math.Abs(Maximum - originalValue);

                if(distanceFromMax < distanceFromLastTick)
                {
                    newValue = Maximum;
                }
            }

        }

        if(Value != newValue)
        {
            Value = newValue;
        }
        else
        {
            // cursor drag will set the position to the cursor, we may need to snap it back
            UpdateThumbPositionAccordingToValue();
        }
        return newValue;
    }

    #region UpdateTo Methods

    protected override void UpdateState()
    {
        if (Visual == null) //don't try to update the UI when the UI is not set yet, mmmmkay?
            return;

        string category = "SliderCategoryState";

        var state = GetDesiredState();

        Visual.SetProperty(category, state);
    }

    private void UpdateThumbPositionAccordingToValue()
    {
        var ratioOver = (Value - Minimum) / (Maximum - Minimum);
        if (Maximum <= Minimum)
        {
            ratioOver = 0;
        }


        // Update December 26, 2022
        // If the thumb uses XUnits of
        // absolute, then if the slider
        // changes, the thumb will be in
        // the old position. By using an X
        // value of percentage, then changes
        // in width won't cause thumb positioning
        // problems:

        //thumb.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        //thumb.X = Microsoft.Xna.Framework.MathHelper.Lerp(0, Track.GetAbsoluteWidth(),
        //    (float)ratioOver);

        thumb.Visual.XUnits = global::Gum.Converters.GeneralUnitType.Percentage;
        thumb.X = 100 * (float)ratioOver;
    }

    protected override void UpdateThumbPositionToCursorDrag(Cursor cursor)
    {
        var valueBefore = Value;

        var cursorScreenX = cursor.GumX();

        var cursorXRelativeToTrack = cursorScreenX - Track.GetAbsoluteX();

        // See UpdateThumbPositionAccordingToValue for an explanation of why we use
        // Percentage rather than PixelsFromSmall:
        //thumb.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        //thumb.X = cursorXRelativeToTrack - cursorGrabOffsetRelativeToThumb;

        thumb.Visual.XUnits = global::Gum.Converters.GeneralUnitType.Percentage;

        var pixelOffset = cursorXRelativeToTrack - cursorGrabOffsetRelativeToThumb;
        var width = Track.GetAbsoluteWidth();
        if(width == 0)
        {
            // prevent divide by 0's
            width = 1;
        }

        thumb.X = 100 * pixelOffset / width;

        float range = Track.GetAbsoluteWidth() ;

        
        if(range != 0)
        {
            var ratio = (thumb.X) / 100;
            ratio = System.Math.Max(0, ratio);
            ratio = System.Math.Min(1, ratio);

            var valueToSet = Minimum + (Maximum - Minimum) * ratio;

            ApplyValueConsideringSnapToTicks(valueToSet);
        }
        else
        {
            Value = Minimum;
        }
        if (valueBefore != Value)
        {
            RaiseValueChangedByUi();
        }
    }

    #endregion

    #region IInputReceiver Methods

    public void OnFocusUpdate()
    {
        var gamepads = GuiManager.GamePadsForUiControl;

        for (int i = 0; i < gamepads.Count; i++)
        {
            var gamepad = gamepads[i];

            HandleGamepadNavigation(gamepad);


            if (gamepad.ButtonRepeatRate(FlatRedBall.Input.Xbox360GamePad.Button.DPadLeft) ||
                gamepad.LeftStick.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Left))
            {
                this.Value -= this.SmallChange;
            }
            else if (gamepad.ButtonRepeatRate(FlatRedBall.Input.Xbox360GamePad.Button.DPadRight) ||
                gamepad.LeftStick.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Right))
            {
                this.Value += this.SmallChange;
            }


            void RaiseIfPushedAndEnabled(FlatRedBall.Input.Xbox360GamePad.Button button)
            {
                if (IsEnabled && gamepad.ButtonPushed(button))
                {
                    ControllerButtonPushed?.Invoke(button);
                }
            }

            RaiseIfPushedAndEnabled(Xbox360GamePad.Button.B);
            RaiseIfPushedAndEnabled(Xbox360GamePad.Button.X);
            RaiseIfPushedAndEnabled(Xbox360GamePad.Button.Y);
            RaiseIfPushedAndEnabled(Xbox360GamePad.Button.Start);
            RaiseIfPushedAndEnabled(Xbox360GamePad.Button.Back);
        }

        var genericGamepads = GuiManager.GenericGamePadsForUiControl;
        for (int i = 0; i < genericGamepads.Count; i++)
        {
            var gamepad = genericGamepads[i];

            HandleGamepadNavigation(gamepad);

            var leftStick = gamepad.AnalogSticks.Length > 0
                ? gamepad.AnalogSticks[0]
                : null;

            if (gamepad.DPadRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Left) ||
                leftStick?.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Left) == true)
            {
                this.Value -= this.SmallChange;
            }
            else if (gamepad.DPadRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Right) ||
                leftStick?.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Right) == true)
            {
                this.Value += this.SmallChange;
            }

            if (IsEnabled)
            {
                for (int buttonIndex = 0; buttonIndex < gamepad.NumberOfButtons; i++)
                {
                    if (gamepad.ButtonPushed(buttonIndex))
                    {
                        GenericGamepadButtonPushed?.Invoke(buttonIndex);
                    }
                }
            }
        }
    }

    public void OnGainFocus()
    {
    }

    public void LoseFocus()
    {
        IsFocused = false;
    }

    public void ReceiveInput()
    {
    }

    public void HandleKeyDown(Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown)
    {
    }

    public void HandleCharEntered(char character)
    {
    }


    #endregion
}
