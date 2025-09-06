using Core.Entities.Concrete;
using System;
using System.Collections.Generic;

namespace Business.Seeds
{
    public static class GroupSeeds
    {
        public static List<Group> GetDefaultGroups()
        {
            return new List<Group>
            {
                new Group 
                { 
                    Id = 1, 
                    GroupName = "Administrators",
                    Description = "System administrators with full access"
                },
                new Group 
                { 
                    Id = 2, 
                    GroupName = "Farmers",
                    Description = "Farmers using plant analysis services"
                },
                new Group 
                { 
                    Id = 3, 
                    GroupName = "Sponsors",
                    Description = "Sponsors providing subscriptions to farmers"
                },
                new Group 
                { 
                    Id = 4, 
                    GroupName = "Support",
                    Description = "Customer support team"
                },
                new Group 
                { 
                    Id = 5, 
                    GroupName = "API Users",
                    Description = "External API integration users"
                }
            };
        }

        public static List<GroupClaim> GetDefaultGroupClaims()
        {
            var groupClaims = new List<GroupClaim>();
            
            // Administrators - Full access (all claims from 1-91)
            for (int claimId = 1; claimId <= 91; claimId++)
            {
                groupClaims.Add(new GroupClaim 
                { 
                    Id = claimId,
                    GroupId = 1, // Administrators
                    OperationClaimId = claimId
                });
            }
            
            // Farmers - Basic plant analysis and profile access
            var farmerClaims = new[] { 5, 10, 11, 14, 21, 41, 90, 91 }; // Farmer role, Plant analysis create/read/list, subscription read, sponsor profile read, mobile access
            int groupClaimId = 100;
            foreach (var claimId in farmerClaims)
            {
                groupClaims.Add(new GroupClaim 
                { 
                    Id = groupClaimId++,
                    GroupId = 2, // Farmers
                    OperationClaimId = claimId
                });
            }
            
            // Sponsors - Sponsorship management and analytics
            var sponsorClaims = new[] { 6, 11, 14, 21, 24, 30, 31, 32, 33, 34, 35, 36, 37, 40, 41, 42, 44, 45, 50, 51, 52, 53, 54, 60, 61, 62, 90, 91 };
            groupClaimId = 200;
            foreach (var claimId in sponsorClaims)
            {
                groupClaims.Add(new GroupClaim 
                { 
                    Id = groupClaimId++,
                    GroupId = 3, // Sponsors
                    OperationClaimId = claimId
                });
            }
            
            // Support - Read access and basic management
            var supportClaims = new[] { 11, 14, 21, 24, 31, 34, 41, 44, 45, 60, 70, 72 };
            groupClaimId = 300;
            foreach (var claimId in supportClaims)
            {
                groupClaims.Add(new GroupClaim 
                { 
                    Id = groupClaimId++,
                    GroupId = 4, // Support
                    OperationClaimId = claimId
                });
            }
            
            // API Users - API access claims
            var apiUserClaims = new[] { 81, 82 }; // Read-only API, Plant Analysis API
            groupClaimId = 400;
            foreach (var claimId in apiUserClaims)
            {
                groupClaims.Add(new GroupClaim 
                { 
                    Id = groupClaimId++,
                    GroupId = 5, // API Users
                    OperationClaimId = claimId
                });
            }
            
            return groupClaims;
        }
    }
}