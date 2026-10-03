using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using TechnicianApp.ViewModels;
using TechnicianApp.Views;

namespace TechnicianApp
{
    public class ViewLocator : IDataTemplate
    {
        public Control? Build(object? param)
        {
            if (param is null)
                return null;

            // Direct strongly-typed mapping for performance & reliability
            if (param is TechnicianAuthViewModel) return new LoginView();
            if (param is TicketQueueViewModel) return new QueueView();
            if (param is TicketDetailViewModel) return new TicketDetailView();
            if (param is WorkshopTrackingViewModel) return new TrackingDialog();
            if (param is InvoiceManagerViewModel) return new InvoiceDialog();

            // Fallback convention-based resolution
            var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            var type = Type.GetType(name);

            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }

            return new TextBlock { Text = "Not Found: " + name };
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
