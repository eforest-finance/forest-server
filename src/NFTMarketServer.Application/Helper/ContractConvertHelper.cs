using System.Collections.Generic;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Proto;
using Google.Protobuf.Collections;
using NFTMarketServer.Inscription;
using AElf.Types;
using Hash = AElf.Types.Hash;
using MerklePath = AElf.Types.MerklePath;
using MerklePathNode = AElf.Types.MerklePathNode;

namespace NFTMarketServer.Helper;

public static class ContractConvertHelper
{
    public static MapField<string, string> ConvertExternalInfoMapField(List<ExternalInfoDto> externalInfoDtos)
    {
        var map = new MapField<string, string>();
        foreach (var externalInfoDto in externalInfoDtos)
        {
            map[externalInfoDto.Key] = externalInfoDto.Value;
        }

        return map;
    }

    public static List<ExternalInfoDto> ConvertExternalInfoDtoList(MapField<string, string> externalInfoDtos)
    {
        var externalInfoDtoList = new List<ExternalInfoDto>();
        if (externalInfoDtos == null)
        {
            return externalInfoDtoList;
        }

        foreach (var key in externalInfoDtos.Keys)
        {
            externalInfoDtoList.Add(new ExternalInfoDto
            {
                Key = key,
                Value = externalInfoDtos[key]
            });
        }

        return externalInfoDtoList;
    }

    public static MerklePath ConvertMerklePath(MerklePathDto merklePathDto)
    {
        var merklePath = new MerklePath();
        foreach (var node in merklePathDto.MerklePathNodes)
        {
            merklePath.MerklePathNodes.Add(new MerklePathNode
            {
                Hash = new Hash { Value = Hash.LoadFromHex(node.Hash).Value },
                IsLeftChildNode = node.IsLeftChildNode
            });
        }

        return merklePath;
    }

    public static MerklePathDto ConvertMerklePathDto(MerklePath merklePath)
    {
        var merklePathDto = new MerklePathDto()
        {
            MerklePathNodes = new List<MerklePathNodeDto>()
        };
        foreach (var node in merklePath.MerklePathNodes)
        {
            merklePathDto.MerklePathNodes.Add(new MerklePathNodeDto
            {
                Hash = node.Hash.ToHex(),
                IsLeftChildNode = node.IsLeftChildNode
            });
        }

        return merklePathDto;
    }
}