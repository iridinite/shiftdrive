/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;

namespace ShiftDrive {

    /// <summary>
    /// Represents a message received by the Intel officer.
    /// </summary>
    internal class CommMessage {

        /// <summary>
        /// Gets or sets the name of the message's sender.
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// Gets or sets the message body.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Gets the timestamp at which the message was received.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Constructs a new empty message.
        /// </summary>
        public CommMessage() {
            Sender = String.Empty;
            Body = String.Empty;
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Constructs a new message.
        /// </summary>
        public CommMessage(string sender, string body) {
            Sender = sender;
            Body = body;
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Returns a string that approximately indicates the message age.
        /// </summary>
        public string GetFuzzyTimestamp() {
            var diff = DateTime.Now - Timestamp;
            return diff.TotalMinutes < 1.0
                ? String.Empty
                : $"{(int)diff.TotalMinutes} min ago";
        }

    }

}
