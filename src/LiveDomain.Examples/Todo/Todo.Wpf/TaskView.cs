﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Todo.Core;
using System.Windows.Input;

namespace Todo.Wpf
{
    [Serializable]
    public class TaskView : ViewModelBase
    {

        private Guid _id;

        public Guid Id
        {
            get { return _id; }
        }

        private string _title;

        public string Title
        {
            get { return _title; }
            set 
            {
                _title = value; 
                NotifyPropertyChanged("Title");
            }
        }

        private string _description;

        public string Description
        {
            get { return _description; }
            set 
            { 
                _description = value;
                NotifyPropertyChanged("Description");
            }
        }

        private DateTime? _dueBy;

        public DateTime? DueBy
        {
            get { return _dueBy; }
            set 
            { 
                _dueBy = value;
                NotifyPropertyChanged("DueBy");
            }
        }

        private DateTime? _completed;

        public DateTime? Completed
        {
            get { return _completed; }
        }

        public bool IsCompleted { 
            get { return Completed.HasValue; }
            set
            {
                if (Completed.HasValue != value)
                {
                    if (value) _completed = DateTime.Now;
                    else _completed = null;
                    NotifyPropertyChanged("Completed");
                    CompleteChanged.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public TaskView(Task task)
        {
            _id = task.Id;
            _title = task.Title;
            _description = task.Description;
            _dueBy = task.DueBy;
            _completed = task.Completed;
        }


        protected override void NotifyPropertyChanged(string propertyName)
        {
            base.NotifyPropertyChanged(propertyName);
            HasBeenModified = true;
        }

        private bool _hasBeenModified;

        public bool HasBeenModified
        {
            get { return _hasBeenModified; }
            private set 
            { 
                _hasBeenModified = value;
                _saveCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// TaskView is instantiated within a query and gets serialized by the engine. 
        /// So this command must be injected after construction.
        /// </summary>
        [NonSerialized]
        DelegateCommand _saveCommand;
        public DelegateCommand SaveCommand { get { return _saveCommand; }  }

        public event EventHandler<EventArgs> CompleteChanged = delegate { };
        public event EventHandler<EventArgs> SaveRequested = delegate { };

        internal void InitializeSaveCommand()
        {
            _saveCommand = new DelegateCommand(() => SaveRequested.Invoke(this, EventArgs.Empty), () => HasBeenModified);
        }
    }
}
