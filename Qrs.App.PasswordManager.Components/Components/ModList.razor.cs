using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor.Utilities;
using Qrs.App.PasswordManager.Client.Models;

namespace MudBlazor
{
    public partial class ModList : MudComponentBase, IDisposable
    {
        protected string Classname =>
        new CssBuilder("mud-list")
           .AddClass("mud-list-padding", !DisablePadding)
          .AddClass(Class)
        .Build();

        [CascadingParameter] protected ModList ParentList { get; set; }

        /// <summary>
        /// The color of the selected List Item.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.List.Appearance)]
        public Color Color { get; set; } = Color.Primary;

        /// <summary>
        /// Child content of component.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.List.Behavior)]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Set true to make the list items clickable. This is also the precondition for list selection to work.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.List.Selecting)]
        public bool Clickable { get; set; }

        /// <summary>
        /// If true, vertical padding will be removed from the list.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.List.Appearance)]
        public bool DisablePadding { get; set; }

        /// <summary>
        /// If true, compact vertical padding will be applied to all list items.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.List.Appearance)]
        public bool Dense { get; set; }

        /// <summary>
        /// If true, the left and right padding is removed on all list items.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.List.Appearance)]
        public bool DisableGutters { get; set; }

        /// <summary>
        /// If true, will disable the list item if it has onclick.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.List.Behavior)]
        public bool Disabled { get; set; }

        /// <summary>
        /// The current selected list item.
        /// Note: make the list Clickable for item selection to work.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.List.Selecting)]
        public ModListItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem == value)
                    return;
                SetSelectedValue(value?.Value, force: true);
            }
        }

        /// <summary>
        /// Called whenever the selection changed
        /// </summary>
        [Parameter] public EventCallback<ModListItem> SelectedItemChanged { get; set; }

        /// <summary>
        /// The current selected value.
        /// Note: make the list Clickable for item selection to work.
        /// </summary>
        [Parameter]
        [Category(CategoryTypes.List.Selecting)]
        public object SelectedValue
        {
            get => _selectedValue;
            set
            {
                SetSelectedValue(value, force: true);
            }
        }

        /// <summary>
        /// Called whenever the selection changed
        /// </summary>
        [Parameter] public EventCallback<object> SelectedValueChanged { get; set; }

        protected override void OnInitialized()
        {
            if (ParentList != null)
            {
                ParentList.Register(this);
                CanSelect = ParentList.CanSelect;
            }
            else
            {
                CanSelect = SelectedItemChanged.HasDelegate || SelectedValueChanged.HasDelegate || SelectedValue != null;
            }
        }

        internal event Action ParametersChanged;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            ParametersChanged?.Invoke();
        }

        private HashSet<ModListItem> _items = new();
        private HashSet<ModList> _childLists = new();
        private ModListItem _selectedItem;
        private object _selectedValue;

        internal void Register(ModListItem item)
        {
            _items.Add(item);
            if (CanSelect && SelectedValue!=null && object.Equals(item.Value, SelectedValue))
            {
                item.SetSelected(true);
                _selectedItem = item;
                SelectedItemChanged.InvokeAsync(item);
            }
        }

        public void StateChanged()
        {
            StateHasChanged();
        }
        public string ItemsToString()
        {
            var sb = new StringBuilder(128);
            foreach (var listItem in _items.ToArray())
            {
                
                sb.Append((listItem.Value as ResourceItem)?.ResourceInfo?.Username ?? "null");
                sb.Append("; ");
                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        internal void Unregister(ModListItem item)
        {
            _items.Remove(item);
        }

        internal void Register(ModList child)
        {
            _childLists.Add(child);
        }

        internal void Unregister(ModList child)
        {
            _childLists.Remove(child);
        }

        internal void SetSelectedValue(object value, bool force = false)
        {
            if ((!CanSelect || !Clickable) && !force)
                return;
            if (object.Equals(_selectedValue, value))
                return;
            _selectedValue = value;
            _ = SelectedValueChanged.InvokeAsync(value);
            _selectedItem = null; // <-- for now, we'll see which item matches the value below
            foreach (var listItem in _items.ToArray())
            {
                var isSelected = value!=null && object.Equals(value, listItem.Value);
                listItem.SetSelected(isSelected);
                if (isSelected)
                    _selectedItem = listItem;
            }
            foreach (var childList in _childLists.ToArray())
            {
                childList.SetSelectedValue(value);
                if (childList.SelectedItem != null)
                    _selectedItem= childList.SelectedItem;
            }
            _ = SelectedItemChanged.InvokeAsync(_selectedItem);
            ParentList?.SetSelectedValue(value);
        }


        public void SetSelectedValueForced(object value)
        {
            var equalValues = object.Equals(_selectedValue, value);
            
            _selectedValue = value;

            
            _selectedItem = null; // <-- for now, we'll see which item matches the value below
            foreach (var listItem in _items.ToArray())
            {
                var isSelected = value != null && object.Equals(value, listItem.Value);
                listItem.SetSelected(isSelected);
                if (isSelected)
                    _selectedItem = listItem;
            }
            foreach (var childList in _childLists.ToArray())
            {
                childList.SetSelectedValue(value);
                if (childList.SelectedItem != null)
                    _selectedItem = childList.SelectedItem;
            }
            _ = SelectedItemChanged.InvokeAsync(_selectedItem);
            if (!equalValues)
            {
                _ = SelectedValueChanged.InvokeAsync(value);
            }
                
            ParentList?.SetSelectedValue(value);
        }


        internal bool CanSelect { get; private set; }

        public void Dispose()
        {
            ParametersChanged = null;
            ParentList?.Unregister(this);
        }

    }
}
