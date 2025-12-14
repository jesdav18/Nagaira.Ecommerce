using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace Nagaira.Ecommerce.Infrastructure.Data;

public class PostgresEnumCommandInterceptor : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        ModifyCommand(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        ModifyCommand(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        ModifyCommand(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ModifyCommand(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    private static void ModifyCommand(DbCommand command)
    {
        if (command.CommandText.Contains("movement_type"))
        {
            var parameters = command.Parameters.Cast<DbParameter>().ToList();
            
            foreach (var param in parameters)
            {
                if (param.Value is string strValue && IsMovementTypeValue(strValue))
                {
                    var paramName = param.ParameterName;
                    command.CommandText = Regex.Replace(
                        command.CommandText,
                        $@"\b{Regex.Escape(paramName)}\b",
                        $"{paramName}::inventory_movement_type",
                        RegexOptions.IgnoreCase);
                }
            }
        }

        foreach (DbParameter param in command.Parameters)
        {
            if (param.Value is DateTime dateTime)
            {
                if (dateTime.Kind != DateTimeKind.Utc)
                {
                    if (dateTime.Kind == DateTimeKind.Unspecified)
                    {
                        param.Value = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                    }
                    else
                    {
                        param.Value = dateTime.ToUniversalTime();
                    }
                }
            }
            else if (param.Value is DateTime?)
            {
                var nullableDateTime = (DateTime?)param.Value;
                if (nullableDateTime.HasValue)
                {
                    var dt = nullableDateTime.Value;
                    if (dt.Kind != DateTimeKind.Utc)
                    {
                        if (dt.Kind == DateTimeKind.Unspecified)
                        {
                            param.Value = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                        }
                        else
                        {
                            param.Value = dt.ToUniversalTime();
                        }
                    }
                }
            }
        }
    }

    private static bool IsMovementTypeValue(string value)
    {
        return value is "purchase" or "sale" or "return" or "adjustment" 
            or "transfer_in" or "transfer_out" or "damage" 
            or "expired" or "initial_stock";
    }
}

