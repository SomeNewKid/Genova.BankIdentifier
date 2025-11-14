// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.BankIdentifier.Terminal;

/// <summary>
/// Console application that classifies the meaning of the word "bank"
/// in user-provided sentences using contextual embeddings.
/// </summary>
public static class Program
{
    /// <summary>
    /// Application entry point. Prompts the user to type sentences containing
    /// the word "bank", classifies their meaning, and displays the result.
    /// The user may continue entering sentences until typing "exit".
    /// </summary>
    /// <param name="args">Command-line arguments (not used).</param>
    public static void Main(string[] args)
    {
        Console.WriteLine("Genova.BankIdentifier.Terminal");
        Console.WriteLine("Type a sentence containing the word \"bank\".");
        Console.WriteLine("Type \"exit\" to quit.");
        Console.WriteLine();

        using BankMeaningIdentifier identifier = new BankMeaningIdentifier();

        while (true)
        {
            Console.Write("> ");
            string? input = Console.ReadLine();

            if (input == null)
            {
                continue;
            }

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Exiting...");
                return;
            }

            BankMeaning meaning = identifier.GetMeaning(input);

            switch (meaning)
            {
                case BankMeaning.None:
                    Console.WriteLine("Does not refer to a bank.");
                    break;

                case BankMeaning.River:
                    Console.WriteLine("Refers to a river bank.");
                    break;

                case BankMeaning.Financial:
                    Console.WriteLine("Refers to a financial bank.");
                    break;

                case BankMeaning.Other:
                    Console.WriteLine("Does not refer to a river bank or a financial bank.");
                    break;

                default:
                    Console.WriteLine("Unable to determine meaning.");
                    break;
            }

            Console.WriteLine();
        }
    }
}
