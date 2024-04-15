module Services.PnLCalculation

open System.Net.Mail
open System

let smtpServerAddress = "smtp.yourserver.com"  // Specify your SMTP server address
let smtpPort = 587  // Common port for SMTP
let smtpUsername = "your_username"  // SMTP server username
let smtpPassword = "your_password"  // SMTP server password

let notifyUserViaEmail (userEmail: string) (messageBody: string) =
    let mail = new MailMessage()
    mail.From <- new MailAddress("your_email@example.com")
    mail.To.Add(new MailAddress(userEmail))
    mail.Subject <- "Alert: P&L Threshold Exceeded"
    mail.Body <- messageBody

    let smtpClient = new SmtpClient(smtpServerAddress, smtpPort)
    smtpClient.EnableSsl <- true
    smtpClient.Credentials <- new System.Net.NetworkCredential(smtpUsername, smtpPassword)
    
    try
        smtpClient.Send(mail)
        true  // Email sent successfully
    with
    | ex -> 
        printfn "Failed to send email: %s" ex.Message
        false  // Email sending failed

