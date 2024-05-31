using FlatRedBall.Forms.Managers;
using FlatRedBall.Input;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace FlatRedBall.Forms.Controls.Games
{
    public enum ConfirmJoinedResult
    {
        Succeeded,
        NotEnoughPlayers,
        InputDeviceNotJoined
    }

    public class ConfirmJoinedEventArgs : EventArgs
    {
        public ConfirmJoinedResult Result { get; private set; }
        public IInputDevice InputDevice { get; private set; }

        public ConfirmJoinedEventArgs(ConfirmJoinedResult result, IInputDevice inputDevice)
        {
            Result = result;
            InputDevice = inputDevice;
        }
    }

    public class InputDeviceSelector : FrameworkElement
    {
        #region Fields/Properties

        GraphicalUiElement InputDeviceContainerInstance;

        public List<IInputDevice> AllConnectedInputDevices { get; private set; }

        /// <summary>
        /// An observable array of joined devices.
        /// </summary>
        public ObservableArray<IInputDevice> JoinedInputDevices { get; private set; }

        int maxPlayers = 4;
        bool hasAssignedMaxPlayers = false;

        /// <summary>
        /// Sets the maximum number of players that can join. 
        /// </summary>
        public int MaxPlayers
        {
            get => maxPlayers;
            set
            {
                maxPlayers = value;
                UpdateToJoinedInputDeviceCount();
            }
        }

        public int MinPlayers { get; set; } = 1;

        List<InputDeviceSelectionItem> InputDeviceSelectionItemsInternal = new List<InputDeviceSelectionItem>();

        /// <summary>
        /// Event raised when the user presses the confirm join button, such as the A button on a gamepad or the Enter key on the keyboard.
        /// This can be used to advance to the next screen, but the ConfirmJoinedEventArgs should be checked to verify that the game can proceed.
        /// </summary>
        public event Action<object, ConfirmJoinedEventArgs> ConfirmedJoinedInput;


        #endregion

        #region Initialize

        public InputDeviceSelector() : base() 
        {
            DoCommonInitialization();
        }

        public InputDeviceSelector(GraphicalUiElement visual) : base(visual) 
        {
            DoCommonInitialization();
        }

        private void DoCommonInitialization()
        {
            // by default we'll support all gamepads + keyboard. This can be changed by the user:
            AllConnectedInputDevices = new List<IInputDevice>
            {
                InputManager.Keyboard,
                InputManager.Xbox360GamePads[0],
                InputManager.Xbox360GamePads[1],
                InputManager.Xbox360GamePads[2],
                InputManager.Xbox360GamePads[3]
            };

        }

        protected override void ReactToVisualChanged()
        {
            // ReactToVisualChanged gets called before DoCommonInitialization
            JoinedInputDevices = new ObservableArray<IInputDevice>(MaxPlayers);
            JoinedInputDevices.CollectionChanged += HandleJoinedInputDevicesChanged;


            InputDeviceContainerInstance = this.Visual.GetGraphicalUiElementByName("InputDeviceContainerInstance");
            // If using the built-in instantiation from the runtime,
            // the Form association is made before variables are assigned.
            // This means that InputDeviceContainerInstance.Children may not
            // yet be populated, and will be populated after - when the "initial state"
            // is set. Therefore, to handle this case there is also an if-check in Activity
            // to make sure there isn't a mismatch.
            InputDeviceContainerInstance.Children.Clear();

            if (!hasAssignedMaxPlayers)
            {
                hasAssignedMaxPlayers = true;
                MaxPlayers = 4;
            }
            else
            {
                UpdateInputDeviceSelectionItemsCount();
            }

            FrameworkElementManager.Self.AddFrameworkElement(this);
            Visual.RemovedFromGuiManager += HandleRemovedFromGuiManager;


            base.ReactToVisualChanged();
        }

        private void HandleRemovedFromGuiManager(object sender, EventArgs e)
        {
            FrameworkElementManager.Self.RemoveFrameworkElement(this);
        }

        #endregion

        #region Update in response to changes

        private void UpdateToJoinedInputDeviceCount()
        {

            if(JoinedInputDevices.Length != MaxPlayers)
            {
                JoinedInputDevices.Resize(MaxPlayers);
            }
            UpdateInputDeviceSelectionItemsCount();
        }

        private void HandleJoinedInputDevicesChanged(object sender, ObservableArrayIndexChangeArgs e)
        {
            var index = e.Index;

            InputDeviceSelectionItemsInternal[index].InputDevice = JoinedInputDevices[index];
        }

        private void UpdateInputDeviceSelectionItemsCount()
        {
            while(InputDeviceSelectionItemsInternal.Count < JoinedInputDevices.Length)
            {
                var newItem = new InputDeviceSelectionItem();
                newItem.InputDeviceRemoved += (sender, args) =>
                {
                    int index = InputDeviceSelectionItemsInternal.IndexOf(newItem);
                    JoinedInputDevices[index] = null;
                };
                InputDeviceSelectionItemsInternal.Add(newItem);
                InputDeviceContainerInstance.Children.Add(newItem.Visual);
                newItem.InputDevice = JoinedInputDevices[InputDeviceSelectionItemsInternal.Count-1];
            }
            while(InputDeviceSelectionItemsInternal.Count > JoinedInputDevices.Length)
            {
                var lastItem = InputDeviceSelectionItemsInternal[InputDeviceSelectionItemsInternal.Count - 1];
                InputDeviceSelectionItemsInternal.Remove(lastItem);
                InputDeviceContainerInstance.Children.Remove(lastItem.Visual);
            }
            // This can happen if an item is directly added to children (such as loaded from .gumx)
            while(InputDeviceContainerInstance.Children.Count > InputDeviceSelectionItemsInternal.Count)
            {
                for(int i = 0; i < InputDeviceContainerInstance.Children.Count; i++)
                {
                    GraphicalUiElement child = (GraphicalUiElement)InputDeviceContainerInstance.Children[i];
                    if(!InputDeviceSelectionItemsInternal.Any(item => item.Visual == child))
                    {
                        InputDeviceContainerInstance.Children.Remove(child);
                    }
                }
            }
        }


        #endregion

        List<IInputDevice> devicesUnjoinedThisFrame = new List<IInputDevice>();
        public override void Activity()
        {
            // See ReactToVisualChanged for why this is necessary
            if(InputDeviceSelectionItemsInternal.Count != MaxPlayers ||
                InputDeviceContainerInstance.Children.Count != MaxPlayers)
            {
                UpdateInputDeviceSelectionItemsCount();
            }
            devicesUnjoinedThisFrame.Clear();

            bool DidUnjoin(IInputDevice inputDevice) =>
                inputDevice.DefaultCancelInput.WasJustPressed|| inputDevice.DefaultBackInput.WasJustPressed;

            foreach (var item in InputDeviceSelectionItemsInternal)
            {
                if(item.InputDevice == null)
                {
                    continue;
                }
                if(item.InputDevice.DefaultCancelInput.WasJustPressed == true || item.InputDevice.DefaultBackInput.WasJustPressed == true)
                {
                    devicesUnjoinedThisFrame.Add(item.InputDevice);
                    item.InputDevice = null;
                }
            }

            foreach(var inputDevice in AllConnectedInputDevices)
            {
                // We use the primary action (such as A or Space) to join,
                // then the Join input (such as Enter or Start) as the "hard"
                // proceed input.
                if(inputDevice.DefaultPrimaryActionInput.WasJustPressed)
                {
                    HandleJoin(inputDevice);
                }
                if(DidUnjoin(inputDevice))
                {
                    // cancel pressed = handle...
                    HandleUnjoin(inputDevice);
                }
            }

            int joinedInputDeviceCount = 0;
            for(int i = 0; i < JoinedInputDevices.Length; i++)
            {
                if (JoinedInputDevices[i] != null )
                {
                    joinedInputDeviceCount++;
                }
            }

            for(int i = 0; i < AllConnectedInputDevices.Count; i++)
            {
                var inputDevice = AllConnectedInputDevices[i];
                // In this context, "Join" means to confirm the selection and advance
                if(inputDevice.DefaultJoinInput.WasJustPressed)
                {
                    ConfirmJoinedResult result = ConfirmJoinedResult.Succeeded;
                    if (JoinedInputDevices.Contains(inputDevice) == false)
                    {
                        result = ConfirmJoinedResult.InputDeviceNotJoined;
                    }
                    else if(joinedInputDeviceCount < MinPlayers)
                    {
                        result = ConfirmJoinedResult.NotEnoughPlayers;
                    }
                    var args = new ConfirmJoinedEventArgs(result, inputDevice);
                    ConfirmedJoinedInput?.Invoke(this, args);
                    break;
                }
            }

            base.Activity();
        }

        private void HandleJoin(IInputDevice inputDevice)
        {
            int? firstEmpty = null;
            for(int i = 0; i < JoinedInputDevices.Length; i++)
            {
                if (JoinedInputDevices[i] == inputDevice)
                {
                    return;
                }
                else if(firstEmpty == null && JoinedInputDevices[i] == null)
                {
                    firstEmpty = i;
                }
            }

            // If we got here, it's not already joined:
            if(firstEmpty != null)
            {
                JoinedInputDevices[firstEmpty.Value] = inputDevice;
            }
        }

        private void HandleUnjoin(IInputDevice inputDevice)
        {
            for(int i = 0; i < JoinedInputDevices.Length; i++)
            {
                if (JoinedInputDevices[i] == inputDevice)
                {
                    JoinedInputDevices[i] = null;
                }
            }
        }
    }

    #region ObservableArray Class 

    public class ObservableArrayIndexChangeArgs
    {
        public int Index { get; private set; }

        public ObservableArrayIndexChangeArgs(int index)
        {
            Index = index;
        }
    }

    public class ObservableArray<T>
    {
        private T[] _array;
        public event Action<object, ObservableArrayIndexChangeArgs> CollectionChanged;

        public int Length => _array.Length;

        public ObservableArray(int size)
        {
            _array = new T[size];
        }

        public T this[int index]
        {
            get => _array[index];
            set
            {
                var oldItem = _array[index];
                _array[index] = value;
                OnIndexChanged(oldItem, index);
            }
        }

        public bool Contains(T item)
        {
            return _array.Contains(item);
        }

        public void Resize(int newSize)
        {
            var newArray = new T[newSize];
            for(int i = 0; i < System.Math.Min(newSize, _array.Length); i++)
            {
                newArray[i] = _array[i];
            }
            _array = newArray;
        }

        void OnIndexChanged(T oldItem, int index)
        {
            CollectionChanged?.Invoke(this, new ObservableArrayIndexChangeArgs(index));
        }
    }

    #endregion
}