using CoreAlign.Application.Tags.Commands;
using CoreAlign.Application.Tags.DTOs;
using CoreAlign.Application.Tags.Mapping;
using CoreAlign.Application.Tags.Queries;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Tags.Handlers;

public class ListTagsQueryHandler : IRequestHandler<ListTagsQuery, IReadOnlyList<TagDto>>
{
    private readonly ITagRepository _repository;
    public ListTagsQueryHandler(ITagRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<TagDto>> Handle(ListTagsQuery request, CancellationToken cancellationToken)
        => (await _repository.ListAsync(request.IsActive, cancellationToken)).Select(TagMapper.ToDto).ToList();
}

public class CreateTagCommandHandler : IRequestHandler<CreateTagCommand, TagDto>
{
    private readonly ITagRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTagCommandHandler(ITagRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TagDto> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        var tag = new Tag(request.Name, request.ColorHex);
        await _repository.AddAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return TagMapper.ToDto(tag);
    }
}

public class UpdateTagCommandHandler : IRequestHandler<UpdateTagCommand, TagDto>
{
    private readonly ITagRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTagCommandHandler(ITagRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TagDto> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Tag not found");

        tag.Update(request.Name, request.ColorHex, request.IsActive);
        _repository.Update(tag);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return TagMapper.ToDto(tag);
    }
}

public class DeleteTagCommandHandler : IRequestHandler<DeleteTagCommand, bool>
{
    private readonly ITagRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTagCommandHandler(ITagRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (tag is null) return false;

        _repository.Remove(tag);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
