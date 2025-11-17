using System;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Constants;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;

namespace Business.Handlers.Farmers.Commands
{
    /// <summary>
    /// Update farmer profile - UserId taken from JWT token for security
    /// </summary>
    public class UpdateFarmerProfileCommand : IRequest<IResult>
    {
        // UserId comes from JWT token in controller, not from user input
        public int UserId { get; set; }

        // Updateable fields
        public string FullName { get; set; }
        public string Email { get; set; }
        public string MobilePhones { get; set; }
        public DateTime? BirthDate { get; set; }
        public int? Gender { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }

        public class UpdateFarmerProfileCommandHandler : IRequestHandler<UpdateFarmerProfileCommand, IResult>
        {
            private readonly IUserRepository _userRepository;

            public UpdateFarmerProfileCommandHandler(IUserRepository userRepository)
            {
                _userRepository = userRepository;
            }

            [SecuredOperation(Priority = 1)]
            [CacheRemoveAspect()]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(UpdateFarmerProfileCommand request, CancellationToken cancellationToken)
            {
                // Get user by JWT userId - ensures users can only update their own profile
                var user = await _userRepository.GetAsync(u => u.UserId == request.UserId);

                if (user == null)
                {
                    return new ErrorResult("Kullanıcı bulunamadı.");
                }

                // Update allowed fields
                user.FullName = request.FullName;
                user.Email = request.Email;
                user.MobilePhones = request.MobilePhones;
                user.BirthDate = request.BirthDate;
                user.Gender = request.Gender;
                user.Address = request.Address;
                user.Notes = request.Notes;
                user.UpdateContactDate = DateTime.Now;

                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();

                return new SuccessResult(Messages.Updated);
            }
        }
    }
}
