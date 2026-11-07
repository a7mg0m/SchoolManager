using AutoMapper;
using BuildingBlocks.CQRS;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.Application
{
    public class GetPaymentByIdQueryHandler : IQueryHandler<GetPaymentByIdQuery, PaymentDto?>
    {
        private readonly IPaymentRepository _repository;
        private readonly IMapper _mapper;

        public GetPaymentByIdQueryHandler(IPaymentRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PaymentDto?> Handle(GetPaymentByIdQuery query, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(query.Id);
            return entity is null ? null : _mapper.Map<PaymentDto>(entity);
        }
    }

    public class GetPaymentsByStudentFeeQueryHandler : IQueryHandler<GetPaymentsByStudentFeeQuery, List<PaymentDto>>
    {
        private readonly IPaymentRepository _repository;
        private readonly IMapper _mapper;

        public GetPaymentsByStudentFeeQueryHandler(IPaymentRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<PaymentDto>> Handle(GetPaymentsByStudentFeeQuery query, CancellationToken cancellationToken)
        {
            var entities = await _repository.SearchAsync(x => x.StudentFeeId == query.StudentFeeId);
            return _mapper.Map<List<PaymentDto>>(entities);
        }
    }

    public class GetAllPaymentsQueryHandler : IQueryHandler<GetAllPaymentsQuery, List<PaymentDto>>
    {
        private readonly IPaymentRepository _repository;
        private readonly IMapper _mapper;

        public GetAllPaymentsQueryHandler(IPaymentRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<PaymentDto>> Handle(GetAllPaymentsQuery query, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<List<PaymentDto>>(entities);
        }
    }
}
