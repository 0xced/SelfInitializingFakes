﻿namespace SelfInitializingFakes
{
    using System;
    using FakeItEasy;

    /// <summary>
    /// Creates self-initializing fakes; fakes that will delegate to an actual service when first
    /// used, creating a script that will be used on subsequent uses.
    /// </summary>
    public static class SelfInitializingFake
    {
        /// <summary>
        /// Creates a new self-initializing fake <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The type of the service to fake.</typeparam>
        /// <param name="serviceFactory">A factory that will create a concrete factory if needed.</param>
        /// <param name="repository">A source of saved call information, or sink for the same.</param>
        /// <returns>A new self-initializing fake <typeparamref name="TService"/>.</returns>
        public static TService For<TService>(Func<TService> serviceFactory, ISavedCallRepository repository)
        {
            if (serviceFactory == null)
            {
                throw new ArgumentNullException(nameof(serviceFactory));
            }

            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            if (repository.LoadCalls() == null)
            {
                var wrappedService = serviceFactory.Invoke();
            }

            return A.Fake<TService>();
        }
    }
}
