using System;
using System.Net.Mail;
using CommerceProject.Services;
using CommerceProject.Utility;

namespace CommerceProject.Model
{
    public class Order
    {
        // you will see that most of the work is done in the "CheckOut" Method.
        public void Checkout(Cart cart, PaymentDetails paymentDetails, bool notifyCustomer)
        {
            // Problem 1: "Cash" Transactions do not need "Credit Card" Processing
            if (paymentDetails.PaymentMethod == PaymentMethod.CreditCard)
            {
                ChargeCard(paymentDetails, cart);
            }

            // Problem 2: Point Of Sale Transactions Do not need Inventory Reservations
            ReserveInventory(cart);

            // Problem 3: Point Of Sale Transactions do not need email notifications
            // the customer does not provide an email
            // the customer knows immediately that the order was success
            if(notifyCustomer)
            {
                NotifyCustomer(cart);
            }

            // Problem 4: Any Change to notifications, Credit Card processing, Or Inventory
            // management will affect "Order" as well as the "Web" and "Point Of Sale"
            // implementations of "Order"
        }

        public void NotifyCustomer(Cart cart)
        {
            string customerEmail = cart.CustomerEmail;
            if (!String.IsNullOrEmpty(customerEmail))
            {
                using (var message = new MailMessage("orders@somewhere.com", customerEmail))
                using (var client = new SmtpClient("localhost"))
                {
                    message.Subject = "Your order placed on " + DateTime.Now.ToString();
                    message.Body = "Your order details: \n " + cart.ToString();

                    try
                    {
                        client.Send(message);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Problem sending notification email", ex);
                    }
                }
            }
        }

        public void ReserveInventory(Cart cart)
        {
            foreach(var item in cart.Items)
            {
                try
                {
                    var inventorySystem = new InventorySystem();
                    inventorySystem.Reserve(item.Sku, item.Quantity);

                }
                catch (InsufficientInventoryException ex)
                {
                    throw new OrderException("Insufficient inventory for item " + item.Sku, ex);
                }
                catch (Exception ex)
                {
                    throw new OrderException("Problem reserving inventory", ex);
                }
            }
        }

        public void ChargeCard(PaymentDetails paymentDetails, Cart cart)
        {
            using (var paymentGateway = new PaymentGateway())
            {
                try
                {
                    paymentGateway.Credentials = "account credentials";
                    paymentGateway.CardNumber = paymentDetails.CreditCardNumber;
                    paymentGateway.ExpiresMonth = paymentDetails.ExpiresMonth;
                    paymentGateway.ExpiresYear = paymentDetails.ExpiresYear;
                    paymentGateway.NameOnCard = paymentDetails.CardholderName;
                    paymentGateway.AmountToCharge = cart.TotalAmount;

                    paymentGateway.Charge();
                }
                catch (AvsMismatchException ex)
                {
                    throw new OrderException("The card gateway rejected the card based on the address provided.", ex);
                }
                catch (Exception ex)
                {
                    throw new OrderException("There was a problem with your card.", ex);
                }
            }
        }
    }
}

    public class OrderException : Exception
    {
        public OrderException(string message, Exception innerException)
            : base(message, innerException)
        {            
        }
    }