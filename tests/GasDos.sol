pragma solidity ^0.5.0;

// #LTLVariables: address user
// #LTLFairness: !started(CrowdFund.refund) U finished(CrowdFund.close) && [](<>(started(CrowdFund.refund, msg.sender == user))) 
// #LTLProperty: [](finished(CrowdFund.buy, msg.sender == user) ==> <>(started(send(from, to, amt), to == user && amt == fsum(CrowdFund.buy, msg.value, msg.sender == user))))

contract CrowdFund {
  address payable[] private refundAddresses;
  mapping(address => uint) public refundAmount;
  bool closed = false;

  function buy() payable public {
    require(!closed);
    require(msg.value > 0);
    
    if(refundAmount[msg.sender] == 0) {
        refundAddresses.push(msg.sender);
    }

    refundAmount[msg.sender] += msg.value;
  }

  function close() public {
    require(!closed);
    closed = true;
  }

  function refund() public {
    require(closed);
    for(uint i = 0; i < refundAddresses.length; i++) {
      if(refundAddresses[i].send(refundAmount[refundAddresses[i]])) {
        refundAmount[refundAddresses[i]] = 0;
      }
    }
  }
}

