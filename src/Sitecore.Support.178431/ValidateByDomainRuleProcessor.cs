// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ValidateByDomainRuleProcessor.cs" company="Sitecore">
//   Copyright (c) Sitecore. All rights reserved.
// </copyright>
// <summary>
//   Defines the ValidateByDomainRuleProcessor type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sitecore.Support.FXM.Pipelines.Tracking.ValidateRequest
{
  using System;
  using System.Linq;

  using Sitecore.Abstractions;
  using Sitecore.Diagnostics;
  using Sitecore.FXM.Abstractions;
  using Sitecore.FXM.Matchers;
  using Sitecore.FXM.Rules.Contexts;

  using Sitecore.FXM.Pipelines.Tracking.ValidateRequest;

  /// <summary>The validate by domain rule processor.</summary>
  public class ValidateByDomainRuleProcessor : AbstractValidateRequestProcessor<ValidateRequestArgs>
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateByDomainRuleProcessor"/> class.
    /// </summary>
    public ValidateByDomainRuleProcessor()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateByDomainRuleProcessor"/> class.
    /// </summary>
    /// <param name="ruleFactory">
    /// The rule factory.
    /// </param>
    /// <param name="configurationFactory">
    /// The configuration factory.
    /// </param>
    /// <param name="domainMatcherRepo">
    /// The domain matcher repo.
    /// </param>
    [Obsolete("This constructor is obsolete and will be removed in the next product version.")]
    public ValidateByDomainRuleProcessor(
        IRuleFactory ruleFactory,
        IConfigurationFactory configurationFactory,
        IDomainMatcherRepository domainMatcherRepo)
      : this(ruleFactory, configurationFactory, domainMatcherRepo, new SitecoreContextWrapper())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateByDomainRuleProcessor"/> class.
    /// </summary>
    /// <param name="ruleFactory">
    /// The rule factory.
    /// </param>
    /// <param name="configurationFactory">
    /// The configuration factory.
    /// </param>
    /// <param name="domainMatcherRepo">
    /// The domain matcher repo.
    /// </param>
    /// <param name="context">The context.</param>
    public ValidateByDomainRuleProcessor(
        IRuleFactory ruleFactory,
        IConfigurationFactory configurationFactory,
        IDomainMatcherRepository domainMatcherRepo, 
        ISitecoreContext context)
      : base(ruleFactory, configurationFactory, domainMatcherRepo, context)
    {
    }

    public override void Process(ValidateRequestArgs args)
    {
      if (args == null)
      {
        throw new ArgumentNullException();
      }

      if (args.IsRequestInternal)
      {
        return;
      }

      base.Process(args);
    }

    /// <summary>Tests the given matcher is a match for the request.</summary>
    /// <param name="args">The arguments.</param>
    /// <param name="ruleContext">The rule context.</param>
    /// <param name="domainMatcherItem">The domain matcher item.</param>
    /// <returns>True if the given domainMatcherItem is a match.</returns>
    protected override bool Match(ValidateRequestArgs args, RequestRuleContext ruleContext, IDomainMatcher domainMatcherItem)
    {
      // if only the primary domain is set, use that to validate, else ignore it and use the rules.
      var primaryDomainCheck = true;
      Assert.IsNotNull(domainMatcherItem.RuleField, "domainMatcherItem.RuleField");
      var domainRuleIsPresent = domainMatcherItem.RuleField.Domains.Any();
      if (!domainRuleIsPresent && !domainMatcherItem.Domain.Equals(ruleContext.Url.Host, StringComparison.InvariantCultureIgnoreCase))
      {
        return false;
      }

      var ruleList = this.RuleFactory.ParseRules<RequestRuleContext>(this.SitecoreContext.Database.Database, domainMatcherItem.RuleField.InnerField.Value);
      if (ruleList.Count == 0)
      {
        return true;
      }

      var hasCondition = false;
      foreach (var rule in ruleList.Rules)
      {
        if (rule.Condition != null)
        {
          hasCondition = true;
        }
      }

      if (!hasCondition)
      {
        return true;
      }

      if (!domainMatcherItem.RuleField.Domains.Any())
      {
        primaryDomainCheck = ruleContext.Url != null && domainMatcherItem.Domain.Equals(ruleContext.Url.Host, StringComparison.InvariantCultureIgnoreCase);
      }

      if (!primaryDomainCheck)
      {
        return false;
      }

      ruleList.Run(ruleContext);

      return ruleContext.IsValid && primaryDomainCheck;
    }

    /// <summary>Tests if this processor should run.</summary>
    /// <param name="args">The arguments.</param>
    /// <param name="ruleContext">The rule context.</param>
    /// <returns>True if this processor should run.</returns>
    protected override bool ShouldProcess(ValidateRequestArgs args, RequestRuleContext ruleContext)
    {
      return args.DomainMatcher == null;
    }
  }
}
