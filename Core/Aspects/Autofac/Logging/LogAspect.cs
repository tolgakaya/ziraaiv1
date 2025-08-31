using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Castle.DynamicProxy;
using Core.CrossCuttingConcerns.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog;
using Core.Utilities.Interceptors;
using Core.Utilities.IoC;
using Core.Utilities.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Core.Aspects.Autofac.Logging
{
    /// <summary>
    /// LogAspect
    /// </summary>
    public class LogAspect : MethodInterception
    {
        private readonly LoggerServiceBase _loggerServiceBase;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Stopwatch _stopwatch;

        public LogAspect(Type loggerService)
        {
            if (loggerService.BaseType != typeof(LoggerServiceBase))
            {
                throw new ArgumentException(AspectMessages.WrongLoggerType);
            }

            _loggerServiceBase = (LoggerServiceBase)ServiceTool.ServiceProvider.GetService(loggerService);
            _httpContextAccessor = ServiceTool.ServiceProvider.GetService<IHttpContextAccessor>();
            _stopwatch = new Stopwatch();
        }

        protected override void OnBefore(IInvocation invocation)
        {
            _stopwatch.Start();
            var logDetail = GetLogDetail(invocation);
            _loggerServiceBase?.Info($"[OPERATION_START] {logDetail}");
        }

        protected override void OnAfter(IInvocation invocation)
        {
            _stopwatch.Stop();
            var executionTime = _stopwatch.ElapsedMilliseconds;
            var logDetail = GetExecutionCompletedDetail(invocation, executionTime);
            _loggerServiceBase?.Info($"[OPERATION_COMPLETED] {logDetail}");
            _stopwatch.Reset();
        }

        protected override void OnException(IInvocation invocation, System.Exception exception)
        {
            _stopwatch.Stop();
            var executionTime = _stopwatch.ElapsedMilliseconds;
            var logDetail = GetExceptionDetail(invocation, exception, executionTime);
            _loggerServiceBase?.Error($"[OPERATION_ERROR] {logDetail}");
            _stopwatch.Reset();
        }

        private string GetLogDetail(IInvocation invocation)
        {
            var logParameters = new List<LogParameter>();
            for (var i = 0; i < invocation.Arguments.Length; i++)
            {
                var parameter = invocation.GetConcreteMethod().GetParameters()[i];
                var value = invocation.Arguments[i];
                
                // Safely handle sensitive parameters
                var parameterValue = IsSensitiveParameter(parameter.Name) 
                    ? "[REDACTED]" 
                    : (value?.ToString() ?? "null");

                logParameters.Add(new LogParameter
                {
                    Name = parameter.Name,
                    Value = parameterValue,
                    Type = parameter.ParameterType.Name,
                });
            }

            var httpContext = _httpContextAccessor.HttpContext;
            var logDetail = new LogDetail
            {
                FullName = $"{invocation.TargetType?.FullName}.{invocation.Method.Name}",
                MethodName = invocation.Method.Name,
                Parameters = logParameters,
                User = httpContext?.User?.Identity?.Name ?? "Anonymous"
            };

            var enhancedLogDetail = new
            {
                Operation = logDetail,
                Context = GetHttpContextInfo(httpContext),
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
            };

            return JsonConvert.SerializeObject(enhancedLogDetail, Formatting.None);
        }

        private string GetExecutionCompletedDetail(IInvocation invocation, long executionTimeMs)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var completionDetail = new
            {
                Operation = $"{invocation.TargetType?.Name}.{invocation.Method.Name}",
                ExecutionTime = $"{executionTimeMs}ms",
                User = httpContext?.User?.Identity?.Name ?? "Anonymous",
                Context = GetHttpContextInfo(httpContext),
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Performance = GetPerformanceCategory(executionTimeMs)
            };

            return JsonConvert.SerializeObject(completionDetail, Formatting.None);
        }

        private string GetExceptionDetail(IInvocation invocation, System.Exception exception, long executionTimeMs)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var exceptionDetail = new
            {
                Operation = $"{invocation.TargetType?.Name}.{invocation.Method.Name}",
                Exception = new
                {
                    Type = exception.GetType().Name,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace?.Split('\n').Take(5).ToArray(), // First 5 lines only
                    InnerException = exception.InnerException?.Message
                },
                ExecutionTime = $"{executionTimeMs}ms",
                User = httpContext?.User?.Identity?.Name ?? "Anonymous",
                Context = GetHttpContextInfo(httpContext),
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Parameters = GetParameterSummary(invocation)
            };

            return JsonConvert.SerializeObject(exceptionDetail, Formatting.None);
        }

        private object GetHttpContextInfo(HttpContext httpContext)
        {
            if (httpContext == null)
                return null;

            try
            {
                return new
                {
                    RequestId = httpContext.TraceIdentifier,
                    Method = httpContext.Request.Method,
                    Path = httpContext.Request.Path.Value,
                    QueryString = httpContext.Request.QueryString.Value,
                    RemoteIP = httpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext.Request.Headers["User-Agent"].ToString()
                };
            }
            catch (System.Exception)
            {
                return new { Error = "Failed to extract HTTP context info" };
            }
        }

        private string GetPerformanceCategory(long executionTimeMs)
        {
            return executionTimeMs switch
            {
                < 100 => "FAST",
                < 500 => "NORMAL", 
                < 1000 => "SLOW",
                < 3000 => "VERY_SLOW",
                _ => "CRITICAL"
            };
        }

        private bool IsSensitiveParameter(string parameterName)
        {
            var sensitiveParams = new[] { "password", "token", "secret", "key", "credential", "authorization" };
            return sensitiveParams.Any(param => parameterName.Contains(param, StringComparison.OrdinalIgnoreCase));
        }

        private object GetParameterSummary(IInvocation invocation)
        {
            return invocation.GetConcreteMethod().GetParameters()
                .Select((param, index) => new 
                { 
                    Name = param.Name, 
                    Type = param.ParameterType.Name,
                    HasValue = invocation.Arguments[index] != null
                }).ToArray();
        }
    }
}