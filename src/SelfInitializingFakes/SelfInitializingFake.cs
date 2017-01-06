﻿namespace SelfInitializingFakes
{
    using System;

    /// <summary>
    /// A fake that will delegate to an actual service when first used, creating a script
    /// that will be used on subsequent uses.
    /// </summary>
    /// <typeparam name="TService">The type of the service to fake.</typeparam>
    public class SelfInitializingFake<TService>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelfInitializingFake{TService}"/> class.
        /// </summary>
        /// <param name="repository">A source of saved call information, or sink for the same.</param>
        /// <param name="serviceFactory">A factory that will create a concrete factory if needed.</param>
        public SelfInitializingFake(ISavedCallRepository repository, Func<TService> serviceFactory)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            if (serviceFactory == null)
            {
                throw new ArgumentNullException(nameof(serviceFactory));
            }
        }
    }
}
