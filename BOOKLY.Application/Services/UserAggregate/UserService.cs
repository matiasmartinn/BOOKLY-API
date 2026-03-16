using AutoMapper;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Common.Validators;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.UserAggregate.DTOs;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Application.Services.UserAggregate
{
    public partial class UserService :  IUserService
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
            ) 
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

        public async Task<Result<UserDto>> Login(LoginDto dto, CancellationToken ct = default)
        {
            var user = await _userRepository.GetByEmail(dto.Email, ct);
            if (user == null ||!user.Password!.Verify(dto.Password, _passwordHasher))
                return Result<UserDto>.Failure(Error.Unauthorized("Credenciales inválidas."));

            return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
        }
        
        public Task<Result<UserDto>> RegisterOwner(CreateUserDto dto, CancellationToken ct = default)
            => CreateUserWithPassword(dto, User.CreateOwner, ct);

        public Task<Result<UserDto>> CreateAdmin(CreateUserDto dto, CancellationToken ct = default)
            => CreateUserWithPassword(dto, User.CreateAdmin, ct);

        // ANALIZAR 2 TRANSACTS
        public async Task<Result<UserDto>> CreateSecretary(int ownerId, CreateSecretaryDto dto, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(dto.ServiceId, ct);
            if (service == null)
                return Result<UserDto>.Failure(Error.NotFound("Service"));

            if (service.OwnerId != ownerId)
                return Result<UserDto>.Failure(Error.Validation("El servicio no pertenece al owner"));


            if (await _userRepository.ExistsByEmail(dto.Email, ct))
                return Result<UserDto>.Failure(Error.Conflict("Ya existe un usuario con ese email"));

            var personName = PersonName.Create(dto.FirstName, dto.LastName);
            var email = Email.Create(dto.Email);
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

            return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
        }

        public async Task<Result<UserDto>> UpdateUser(int id, UpdateUserDto dto, CancellationToken ct = default)
        {
            var user = await _userRepository.GetOne(id, ct);
            if (user is null)
                return Result<UserDto>.Failure(Error.NotFound("Usuario"));

            user.ChangeUserName(PersonName.Create(dto.FirstName, dto.LastName));
            user.ChangeEmail(Email.Create(dto.Email));
            _userRepository.Update(user);
            await _unitOfWork.SaveChanges(ct);
            return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
        }

        public async Task<Result> DeleteUser(int id, CancellationToken ct = default)
        {
            var user = await _userRepository.GetOne(id, ct);
            if (user is null)
                return Result.Failure(Error.NotFound("Usuario"));

            _userRepository.Remove(user);
            await _unitOfWork.SaveChanges(ct);
            return Result.Success();
        }

        // ANALIZAR.
        public async Task<Result<UserDto>> CompleteInvitation(CompleteSecretaryInvitationDto dto, CancellationToken ct = default)
        {
            var now = DateTime.Now;

            if (string.IsNullOrWhiteSpace(dto.Token))
                return Result<UserDto>.Failure(Error.Validation("Token requerido"));

            var passwordValidation = PasswordValidator.Validate(dto.Password);
            if (!passwordValidation.IsSuccess)
                return Result<UserDto>.Failure(passwordValidation.Error!);

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

            var hash = _passwordHasher.Hash(dto.Password);
            user.ChangePassword(Password.FromHash(hash));

            invitation.MarkAsUsed(now);
            user.Activate();
            _userInvitationRepository.Update(invitation);
            _userRepository.Update(user);

            await _unitOfWork.SaveChanges(ct);

            return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
        }

        private async Task<Result<UserDto>> CreateUserWithPassword(
            CreateUserDto dto,
            Func<PersonName, Email, Password, User> factory,
            CancellationToken ct)
        {
            if (await _userRepository.ExistsByEmail(dto.Email, ct))
                return Result<UserDto>.Failure(Error.Conflict("Email ya está registrado."));

            var email = Email.Create(dto.Email);
            var personName = PersonName.Create(dto.FirstName, dto.LastName);

            var passwordValidation = PasswordValidator.Validate(dto.Password);
            if (!passwordValidation.IsSuccess)
                return Result<UserDto>.Failure(passwordValidation.Error!);

            var password = Password.FromHash(_passwordHasher.Hash(dto.Password));
            var user = factory(personName, email, password);
            await _userRepository.AddOne(user, ct);
            await _unitOfWork.SaveChanges(ct);

            return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
        }
    }
}
