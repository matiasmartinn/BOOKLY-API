using AutoMapper;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.UserAggregate.DTOs;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;
using BOOKLY.Application.Common;
using Microsoft.Extensions.Logging;

namespace BOOKLY.Application.Services.UserAggregate
{
    public partial class UserService : BaseService<UserService> ,IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserInvitationRepository _userInvitationRepository;
        private readonly IInvitationTokenGenerator _invitationTokenGenerator;
        private readonly ITokenHashingService _tokenHashingService;
        private readonly IMapper _mapper;
    
        public UserService(
            IUserRepository userRepository
            , IServiceRepository serviceRepository
            ,IUnitOfWork unitOfWork
            ,IPasswordHasher passwordHasher
            ,ITokenHashingService tokenHashingService
            ,IInvitationTokenGenerator invitationTokenGenerator
            , IUserInvitationRepository userInvitationRepository
            , IMapper mapper
            , ILogger<UserService> logger) 
            : base(logger)
        {
            _userRepository = userRepository;
            _serviceRepository = serviceRepository;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _tokenHashingService = tokenHashingService;
            _invitationTokenGenerator = invitationTokenGenerator;
            _userInvitationRepository = userInvitationRepository;
            _mapper = mapper;
        }

        public async Task<Result<UserDto>> GetUserById(int id, CancellationToken ct = default)
        {
            if (id <= 0)
                return Result<UserDto>.Failure(Error.Validation("Id inválido"));

            var user = await _userRepository.GetOne(id, ct);
            if (user is null)
                return Result<UserDto>.Failure(Error.NotFound("Usuario"));

            return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
        }

        public async Task<Result<UserDto>> RegisterOwner(CreateUserDto dto, CancellationToken ct = default)
        {
            var email = Email.Create(dto.Email);
            var personName = PersonName.Create(dto.FirstName, dto.LastName);

            Password.AssertPlainTextIsValid(dto.Password);
            var hashedPassword = _passwordHasher.Hash(dto.Password);
            var password = Password.FromHash(hashedPassword);

            if (await _userRepository.ExistsByEmail(email, ct))
                return Result<UserDto>.Failure(Error.Conflict("Email ya está registrado."));

            return await Execute(async () =>
            {
                var user = User.RegisterAsOwner(personName, email, password);

                await _userRepository.AddOne(user, ct);
                await _unitOfWork.SaveChanges(ct);
                return _mapper.Map<UserDto>(user);
            });
        }

        public async Task<Result<UserDto>> CreateAdmin(CreateUserDto dto, CancellationToken ct = default)
        {
            var email = Email.Create(dto.Email);
            var personName = PersonName.Create(dto.FirstName, dto.LastName);

            Password.AssertPlainTextIsValid(dto.Password);
            var hashedPassword = _passwordHasher.Hash(dto.Password);
            var password = Password.FromHash(hashedPassword);

            if (await _userRepository.ExistsByEmail(email, ct))
                return Result<UserDto>.Failure(Error.Conflict("Email ya está registrado."));

            return await Execute(async () =>
            {
                var user = User.CreateAdmin(personName, email, password);

                await _userRepository.AddOne(user, ct);
                await _unitOfWork.SaveChanges(ct);
                return _mapper.Map<UserDto>(user);
            });
        }

        public async Task<Result<UserDto>> CreateSecretary(int ownerId, CreateSecretaryDto dto, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(dto.ServiceId, ct);
            if (service == null)
                return Result<UserDto>.Failure(Error.NotFound("Service"));

            if (service.OwnerId != ownerId)
                return Result<UserDto>.Failure(Error.Validation("El servicio no pertenece al owner"));

            var email = Email.Create(dto.Email);

            if (await _userRepository.ExistsByEmail(email, ct))
                return Result<UserDto>.Failure(Error.Conflict("Ya existe un usuario con ese email"));

            var personName = PersonName.Create(dto.FirstName, dto.LastName);

            return await Execute(async () =>
            {

                var user = User.CreateSecretary(personName, email);
                await _userRepository.AddOne(user, ct);
                await _unitOfWork.SaveChanges(ct);

                service.AssignSecretary(user.Id);
                _serviceRepository.Update(service);

                var rawToken = _invitationTokenGenerator.GenerateToken();
                var tokenHash = _tokenHashingService.HashToken(rawToken);

                var invitation = UserInvitation.Create(user.Id, tokenHash, DateTime.Now, TimeSpan.FromHours(24));
                await _userInvitationRepository.AddOne(invitation, ct);
                await _unitOfWork.SaveChanges(ct);

                return _mapper.Map<UserDto>(user);
            });
        }

        public async Task<Result<UserDto>> UpdateUser(int id, UpdateUserDto dto, CancellationToken ct = default)
        {
            var user = await _userRepository.GetOne(id, ct);
            if (user is null)
                return Result<UserDto>.Failure(Error.NotFound("Usuario"));

            return await Execute(async () =>
            {
                user.ChangeUserName(PersonName.Create(dto.FirstName, dto.LastName));
                user.ChangeEmail(Email.Create(dto.Email));
                _userRepository.Update(user);
                await _unitOfWork.SaveChanges(ct);
                return _mapper.Map<UserDto>(user);
            });
        }

        public async Task<Result> DeleteUser(int id, CancellationToken ct = default)
        {
            var user = await _userRepository.GetOne(id, ct);
            if (user is null)
                return Result.Failure(Error.NotFound("Usuario"));

            return await Execute(async () =>
            {
                _userRepository.Remove(user);
                await _unitOfWork.SaveChanges(ct);
            });
        }

        public async Task<Result<UserDto>> CompleteInvitation(CompleteSecretaryInvitationDto dto, CancellationToken ct = default)
        {
            var now = DateTime.Now;

            if (string.IsNullOrWhiteSpace(dto.Token))
                return Result<UserDto>.Failure(Error.Validation("Token requerido"));

            Password.AssertPlainTextIsValid(dto.Password);
            var tokenHash = _tokenHashingService.HashToken(dto.Token);
            var invitation = await _userInvitationRepository.GetByTokenHash(tokenHash, ct);
            if (invitation is null)
                return Result<UserDto>.Failure(Error.NotFound("Invitación"));

            if (invitation.IsUsed)
                return Result<UserDto>.Failure(Error.Validation("La invitación ya fue utilizada"));

            if (invitation.IsExpired(now))
                return Result<UserDto>.Failure(Error.Validation("La invitación está vencida"));

            var user = await _userRepository.GetOne(invitation.UserId, ct);
            if (user is null)
                return Result<UserDto>.Failure(Error.NotFound("Usuario"));

            return await Execute(async () =>
            {
                var hash = _passwordHasher.Hash(dto.Password);
                user.ChangePassword(Password.FromHash(hash));

                invitation.MarkAsUsed(now);
                user.Activate();
                _userInvitationRepository.Update(invitation);
                _userRepository.Update(user);

                await _unitOfWork.SaveChanges(ct);

                return _mapper.Map<UserDto>(user);
            });
        }
    }
}
