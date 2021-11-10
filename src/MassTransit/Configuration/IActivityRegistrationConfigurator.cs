namespace MassTransit
{
    using System;
    using Courier;
    using Registration;


    public interface IActivityRegistrationConfigurator<TActivity, TArguments, TLog> :
        IActivityRegistrationConfigurator
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
    }


    public interface IActivityRegistrationConfigurator
    {
        /// <summary>
        /// Configure both the execute and compensate endpoints in a single call. Separate calls have been added, which
        /// may ultimately cause this method to be deprecated.
        /// </summary>
        /// <param name="configureExecute"></param>
        /// <param name="configureCompensate"></param>
        void Endpoints(Action<IExecuteActivityEndpointRegistrationConfigurator> configureExecute,
            Action<ICompensateActivityEndpointRegistrationConfigurator> configureCompensate);

        /// <summary>
        /// Configure the activity's execute endpoint
        /// </summary>
        /// <param name="configureExecute"></param>
        IActivityRegistrationConfigurator ExecuteEndpoint(Action<IExecuteActivityEndpointRegistrationConfigurator> configureExecute);

        /// <summary>
        /// Configure the activity's compensate endpoint
        /// </summary>
        /// <param name="configureCompensate"></param>
        IActivityRegistrationConfigurator CompensateEndpoint(Action<ICompensateActivityEndpointRegistrationConfigurator> configureCompensate);
    }
}
