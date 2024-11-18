﻿using FlatRedBall.Gui;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace FlatRedBall.Forms.Controls;

public class PasswordBox : TextBoxBase
{
    #region Fields/Properties

    SecureString securePassword = new SecureString();
    public SecureString SecurePassword
    {
        get { return securePassword; }
    }
    public string Password
    {
        get
        {
            return SecureStringToString(SecurePassword);

        }
        set
        {
            SecurePassword.Clear();
            if (value != null)
            {
                foreach (var character in value)
                {
                    SecurePassword.AppendChar(character);
                }
            }
            CallMethodsInResponseToPasswordChanged();
        }
    }

    String SecureStringToString(SecureString value)
    {
        IntPtr valuePtr = IntPtr.Zero;
        try
        {
            valuePtr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(value);
            return System.Runtime.InteropServices.Marshal.PtrToStringUni(valuePtr);
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
        }
    }


    // Update Gum's default to include this first:
    //public char PasswordChar { get; set; } = '●';
    public char PasswordChar { get; set; } = '*';

    public event EventHandler PasswordChanged;

    protected override string DisplayedText
    {
        get
        {
            return new string(PasswordChar, SecurePassword.Length);
        }
    }

    protected override string CategoryName => "PasswordBoxCategoryState";


    #endregion

    #region Initialize Methods

    public PasswordBox() : base() { }

    public PasswordBox(GraphicalUiElement visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();

        if(selectionInstance != null)
        {
            selectionInstance.Visible = false;
        }

        UpdateDisplayedCharacters();
    }
    #endregion

    #region Event Handler Methods

    public override void HandleCharEntered(char character)
    {
        // See TextBox on why we don't check IsFocused
        //if (HasFocus)
        {
            if(selectionLength != 0)
            {
                DeleteSelection();
            }
            // If text is null force it to be an empty string so we can add characters

            if (character == '\b'
                // I think CTRL Backspace?
                || character == (char)127
                // esc
                || character == (char)27)
            {
                // do nothing, handled with a backspace above
                //    HandleBackspace();
            }
            else if (character == '\r' || character == '\n')
            {
                // no enter supported on passwords, do we send an event?
            }
            else
            {
                InsertCharacterAtIndex(character, caretIndex);
                caretIndex++;

                CallMethodsInResponseToPasswordChanged();
            }

        }
    }

    private void CallMethodsInResponseToPasswordChanged()
    {
        TruncateTextToMaxLength();
        UpdateCaretPositionToCaretIndex();
        OffsetTextToKeepCaretInView();
        UpdateDisplayedCharacters();
        UpdatePlaceholderVisibility();
        PasswordChanged?.Invoke(this, null);
        PushValueToViewModel();
    }

    private void InsertCharacterAtIndex(char character, int caretIndex)
    {
        this.SecurePassword.InsertAt(caretIndex, character);
    }

    public override void HandleBackspace(bool isCtrlDown = false)
    {
        if (caretIndex > 0 || SelectionLength > 0)
        {
            if (selectionLength > 0)
            {
                DeleteSelection();
            }
            else if (isCtrlDown)
            {
                for (int i = caretIndex - 1; i > -1; i--)
                {
                    SecurePassword.RemoveAt(i);
                }

                caretIndex = 0;
            }
            else
            {
                var whereToRemoveFrom = caretIndex - 1;
                // Move the care to the left one before removing from the text. Otherwise, if the
                // caret is at the end of the word, modifying the word will shift the caret to the left, 
                // and that could cause it to shift over two times.
                caretIndex--;
                SecurePassword.RemoveAt(whereToRemoveFrom);
            }
            CallMethodsInResponseToPasswordChanged();
        }
    }

    public void DeleteSelection()
    {
        for(int i = 0; i < SelectionLength; i++)
        {
            SecurePassword.RemoveAt(selectionStart);

        }
        CallMethodsInResponseToPasswordChanged();

        CaretIndex = selectionStart;
        SelectionLength = 0;
    }

    protected override void HandleDelete()
    {
        if (caretIndex < (SecurePassword?.Length ?? 0))
        {
            SecurePassword.RemoveAt(caretIndex);

            CallMethodsInResponseToPasswordChanged();
        }
    }

    public void Clear()
    {
        SecurePassword.Clear();
        CallMethodsInResponseToPasswordChanged();
    }

    protected override void HandlePaste()
    {

        var whatToPaste = Clipboard.ClipboardImplementation.GetText();
        if (!string.IsNullOrEmpty(whatToPaste))
        {
            if (selectionLength != 0)
            {
                DeleteSelection();
            }
            foreach (var character in whatToPaste)
            {
                InsertCharacterAtIndex(character, caretIndex);
                caretIndex++;
            }
            CallMethodsInResponseToPasswordChanged();
        }
    }

    private void UpdateDisplayedCharacters()
    {
        var newText = new string(PasswordChar, SecurePassword.Length);
        if (this.coreTextObject.RawText != newText)
        {
            textComponent.SetProperty("Text", newText);

            CaretIndex = System.Math.Min(CaretIndex, Password?.Length ?? 0);

        }
    }

#endregion

    public override void SelectAll()
    {
        if (this.DisplayedText != null)
        {
            this.SelectionStart = 0;
            this.SelectionLength = this.DisplayedText.Length;
        }
    }

    protected override void TruncateTextToMaxLength()
    {
        while(SecurePassword.Length > MaxLength)
        {
            SecurePassword.RemoveAt(SecurePassword.Length-1);
        }
    }
}
