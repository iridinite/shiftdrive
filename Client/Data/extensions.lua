function math.dist(posa, posb)
  return math.sqrt(math.pow(posa[1] - posb[1], 2) + math.pow(posa[2] - posb[2], 2))
end

function math.distsq(posa, posb)
  return math.pow(posa[1] - posb[1], 2) + math.pow(posa[2] - posb[2], 2)
end